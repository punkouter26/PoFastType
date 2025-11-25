using Azure;
using Azure.Data.Tables;
using PoFastType.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PoFastType.Api.Repositories;

/// <summary>
/// Azure Table Storage implementation for keystroke data repository
/// Follows Repository Pattern and Single Responsibility Principle
/// </summary>
public class AzureTableKeystrokeRepository : IKeystrokeRepository
{
    private readonly TableClient _tableClient;
    private readonly ILogger<AzureTableKeystrokeRepository> _logger;
    private const string TableName = "PoFastTypeKeystrokes";

    public AzureTableKeystrokeRepository(IConfiguration configuration, ILogger<AzureTableKeystrokeRepository> logger)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        var connectionString = configuration["AzureTableStorage:ConnectionString"]
            ?? throw new InvalidOperationException("Azure Table Storage connection string is required");

        _tableClient = new TableClient(connectionString, TableName);
        _logger = logger;
    }

    public async Task<KeystrokeData> AddKeystrokeAsync(KeystrokeData keystroke)
    {
        try
        {
            if (keystroke == null)
                throw new ArgumentNullException(nameof(keystroke));

            await _tableClient.CreateIfNotExistsAsync();

            // Ensure RowKey is set (GameId_SequenceNumber format)
            if (string.IsNullOrEmpty(keystroke.RowKey))
            {
                keystroke.RowKey = $"{keystroke.GameId}_{keystroke.SequenceNumber:D6}";
            }

            var entity = MapToTableEntity(keystroke);
            await _tableClient.AddEntityAsync(entity);

            _logger.LogDebug("Keystroke added for user {UserId}, game {GameId}, sequence {Seq}",
                keystroke.PartitionKey, keystroke.GameId, keystroke.SequenceNumber);

            return keystroke;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add keystroke for user {UserId}", keystroke?.PartitionKey ?? "null");
            throw;
        }
    }

    public async Task AddKeystrokesBatchAsync(IEnumerable<KeystrokeData> keystrokes)
    {
        if (keystrokes == null || !keystrokes.Any())
        {
            _logger.LogWarning("Attempted to add empty keystroke batch");
            return;
        }

        try
        {
            await _tableClient.CreateIfNotExistsAsync();

            var keystrokeList = keystrokes.ToList();
            var userId = keystrokeList.First().PartitionKey;

            // Azure Table Storage batch operations limited to 100 entities and same partition key
            var batches = keystrokeList
                .GroupBy(k => k.PartitionKey)
                .SelectMany(g => g.Select((k, i) => new { Index = i, Keystroke = k })
                    .GroupBy(x => x.Index / 100)
                    .Select(batch => batch.Select(x => x.Keystroke).ToList()))
                .ToList();

            foreach (var batch in batches)
            {
                var batchOperations = new List<TableTransactionAction>();

                foreach (var keystroke in batch)
                {
                    // Ensure RowKey is set
                    if (string.IsNullOrEmpty(keystroke.RowKey))
                    {
                        keystroke.RowKey = $"{keystroke.GameId}_{keystroke.SequenceNumber:D6}";
                    }

                    var entity = MapToTableEntity(keystroke);
                    batchOperations.Add(new TableTransactionAction(TableTransactionActionType.Add, entity));
                }

                if (batchOperations.Any())
                {
                    await _tableClient.SubmitTransactionAsync(batchOperations);
                }
            }

            _logger.LogInformation("Added {Count} keystrokes in batch for user {UserId}",
                keystrokeList.Count, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add keystroke batch");
            throw;
        }
    }

    public async Task<IEnumerable<KeystrokeData>> GetUserKeystrokesAsync(string userId, int? limit = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        try
        {
            await _tableClient.CreateIfNotExistsAsync();

            var filter = $"PartitionKey eq '{userId}'";
            var query = _tableClient.QueryAsync<TableEntity>(filter: filter);
            var results = new List<KeystrokeData>();

            await foreach (var entity in query)
            {
                results.Add(MapFromTableEntity(entity));

                if (limit.HasValue && results.Count >= limit.Value)
                    break;
            }

            _logger.LogInformation("Retrieved {Count} keystrokes for user {UserId}", results.Count, userId);
            return results.OrderBy(k => k.RecordedAt).ThenBy(k => k.SequenceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve keystrokes for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<KeystrokeData>> GetGameKeystrokesAsync(string userId, string gameId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(gameId))
            throw new ArgumentException("Game ID cannot be null or empty", nameof(gameId));

        try
        {
            await _tableClient.CreateIfNotExistsAsync();

            var filter = $"PartitionKey eq '{userId}' and GameId eq '{gameId}'";
            var query = _tableClient.QueryAsync<TableEntity>(filter: filter);
            var results = new List<KeystrokeData>();

            await foreach (var entity in query)
            {
                results.Add(MapFromTableEntity(entity));
            }

            _logger.LogInformation("Retrieved {Count} keystrokes for game {GameId}", results.Count, gameId);
            return results.OrderBy(k => k.SequenceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve keystrokes for game {GameId}", gameId);
            throw;
        }
    }

    public async Task<IEnumerable<KeystrokeData>> GetUserKeystrokesInRangeAsync(string userId, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        try
        {
            await _tableClient.CreateIfNotExistsAsync();

            var filter = $"PartitionKey eq '{userId}' and RecordedAt ge datetime'{startDate:yyyy-MM-ddTHH:mm:ssZ}' and RecordedAt le datetime'{endDate:yyyy-MM-ddTHH:mm:ssZ}'";
            var query = _tableClient.QueryAsync<TableEntity>(filter: filter);
            var results = new List<KeystrokeData>();

            await foreach (var entity in query)
            {
                results.Add(MapFromTableEntity(entity));
            }

            _logger.LogInformation("Retrieved {Count} keystrokes for user {UserId} between {Start} and {End}",
                results.Count, userId, startDate, endDate);
            return results.OrderBy(k => k.RecordedAt).ThenBy(k => k.SequenceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve keystrokes in range for user {UserId}", userId);
            throw;
        }
    }

    public async Task DeleteUserKeystrokesAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        try
        {
            await _tableClient.CreateIfNotExistsAsync();

            var filter = $"PartitionKey eq '{userId}'";
            var query = _tableClient.QueryAsync<TableEntity>(filter: filter);
            var deleteCount = 0;

            var entitiesToDelete = new List<TableEntity>();
            await foreach (var entity in query)
            {
                entitiesToDelete.Add(entity);
            }

            // Delete in batches of 100 (Azure Table Storage limit)
            var batches = entitiesToDelete
                .Select((entity, index) => new { Index = index, Entity = entity })
                .GroupBy(x => x.Index / 100)
                .Select(g => g.Select(x => x.Entity).ToList())
                .ToList();

            foreach (var batch in batches)
            {
                var batchOperations = batch
                    .Select(entity => new TableTransactionAction(TableTransactionActionType.Delete, entity))
                    .ToList();

                if (batchOperations.Any())
                {
                    await _tableClient.SubmitTransactionAsync(batchOperations);
                    deleteCount += batchOperations.Count;
                }
            }

            _logger.LogInformation("Deleted {Count} keystrokes for user {UserId}", deleteCount, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete keystrokes for user {UserId}", userId);
            throw;
        }
    }

    public async Task<int> GetKeystrokeCountAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        try
        {
            await _tableClient.CreateIfNotExistsAsync();

            var filter = $"PartitionKey eq '{userId}'";
            var query = _tableClient.QueryAsync<TableEntity>(filter: filter, select: new[] { "PartitionKey" });
            var count = 0;

            await foreach (var _ in query)
            {
                count++;
            }

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get keystroke count for user {UserId}", userId);
            throw;
        }
    }

    private static TableEntity MapToTableEntity(KeystrokeData keystroke)
    {
        var entity = new TableEntity(keystroke.PartitionKey, keystroke.RowKey)
        {
            ["GameId"] = keystroke.GameId,
            ["SequenceNumber"] = keystroke.SequenceNumber,
            ["Key"] = keystroke.Key,
            ["ExpectedChar"] = keystroke.ExpectedChar,
            ["IsCorrect"] = keystroke.IsCorrect,
            ["IsBackspace"] = keystroke.IsBackspace,
            ["ElapsedMs"] = keystroke.ElapsedMs,
            ["IntervalMs"] = keystroke.IntervalMs,
            ["TextPosition"] = keystroke.TextPosition,
            ["CurrentWPM"] = keystroke.CurrentWPM,
            ["CurrentAccuracy"] = keystroke.CurrentAccuracy,
            ["RecordedAt"] = keystroke.RecordedAt
        };

        return entity;
    }

    private static KeystrokeData MapFromTableEntity(TableEntity entity)
    {
        return new KeystrokeData
        {
            PartitionKey = entity.PartitionKey,
            RowKey = entity.RowKey,
            GameId = entity.GetString("GameId") ?? string.Empty,
            SequenceNumber = entity.GetInt32("SequenceNumber") ?? 0,
            Key = entity.GetString("Key") ?? string.Empty,
            ExpectedChar = entity.GetString("ExpectedChar") ?? string.Empty,
            IsCorrect = entity.GetBoolean("IsCorrect") ?? false,
            IsBackspace = entity.GetBoolean("IsBackspace") ?? false,
            ElapsedMs = entity.GetInt64("ElapsedMs") ?? 0,
            IntervalMs = entity.GetInt64("IntervalMs") ?? 0,
            TextPosition = entity.GetInt32("TextPosition") ?? 0,
            CurrentWPM = entity.GetDouble("CurrentWPM") ?? 0,
            CurrentAccuracy = entity.GetDouble("CurrentAccuracy") ?? 0,
            RecordedAt = entity.GetDateTime("RecordedAt") ?? DateTime.UtcNow,
            Timestamp = entity.Timestamp,
            ETag = entity.ETag
        };
    }
}
