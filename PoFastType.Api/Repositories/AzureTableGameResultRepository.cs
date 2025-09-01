using Azure.Data.Tables;
using PoFastType.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PoFastType.Api.Repositories;

/// <summary>
/// Repository Pattern (GoF) Implementation
/// Single Responsibility Principle (SOLID) - Handles only Azure Table Storage operations for game results
/// Dependency Inversion Principle (SOLID) - Depends on abstractions (IConfiguration, ILogger) not concretions
/// </summary>
public class AzureTableGameResultRepository : IGameResultRepository
{
    private readonly TableClient _tableClient;
    private readonly ILogger<AzureTableGameResultRepository> _logger; public AzureTableGameResultRepository(IConfiguration configuration, ILogger<AzureTableGameResultRepository> logger)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        var connectionString = configuration["AzureTableStorage:ConnectionString"]
            ?? throw new InvalidOperationException("Azure Table Storage connection string is required");
        var tableName = configuration["AzureTableStorage:TableName"]
            ?? throw new InvalidOperationException("Azure Table Storage table name is required");

        _tableClient = new TableClient(connectionString, tableName);
        _logger = logger;
    }
    public async Task<GameResult> AddAsync(GameResult gameResult)
    {
        try
        {
            if (gameResult == null)
                throw new ArgumentNullException(nameof(gameResult));

            // Set GameTimestamp if not already set
            if (gameResult.GameTimestamp == default)
                gameResult.GameTimestamp = DateTime.UtcNow;

            // Generate RowKey if not set (reverse timestamp for descending order)
            if (string.IsNullOrEmpty(gameResult.RowKey))
                gameResult.RowKey = (DateTime.MaxValue.Ticks - gameResult.GameTimestamp.Ticks).ToString("D19");

            await _tableClient.CreateIfNotExistsAsync();

            var entity = new TableEntity(gameResult.PartitionKey, gameResult.RowKey)
            {
                ["Username"] = gameResult.Username,
                ["NetWPM"] = gameResult.NetWPM,
                ["Accuracy"] = gameResult.Accuracy,
                ["GrossWPM"] = gameResult.GrossWPM,
                ["CompositeScore"] = gameResult.CompositeScore, // Add missing composite score
                ["ProblemKeysJson"] = gameResult.ProblemKeysJson,
                ["GameTimestamp"] = gameResult.GameTimestamp
            };

            await _tableClient.AddEntityAsync(entity);

            _logger.LogInformation("Game result added for user {UserId}: NetWPM={NetWPM}, Accuracy={Accuracy}%",
                gameResult.PartitionKey, gameResult.NetWPM, gameResult.Accuracy);

            return gameResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add game result for user {UserId}", gameResult?.PartitionKey ?? "null");
            throw;
        }
    }
    public async Task<IEnumerable<GameResult>> GetUserResultsAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        try
        {
            await _tableClient.CreateIfNotExistsAsync();

            var query = _tableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{userId}'");
            var results = new List<GameResult>();

            await foreach (var entity in query)
            {
                results.Add(MapFromTableEntity(entity));
            }
            _logger.LogInformation("Retrieved {Count} game results for user {UserId}", results.Count, userId);
            return results.OrderByDescending(r => r.GameTimestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve game results for user {UserId}", userId);
            throw;
        }
    }
    public async Task<IEnumerable<GameResult>> GetTopResultsAsync(int count)
    {
        if (count <= 0 || count > 100)
            throw new ArgumentException("Count must be between 1 and 100", nameof(count));

        try
        {
            await _tableClient.CreateIfNotExistsAsync();

            var query = _tableClient.QueryAsync<TableEntity>();
            var allResults = new List<GameResult>();

            await foreach (var entity in query)
            {
                allResults.Add(MapFromTableEntity(entity));
            }

            var topResults = allResults
                .OrderByDescending(r => r.CompositeScore) // Changed from NetWPM to CompositeScore
                .Take(count)
                .ToList();

            _logger.LogInformation("Retrieved top {Count} game results", topResults.Count);
            return topResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve top game results");
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string userId, string rowKey)
    {
        try
        {
            await _tableClient.CreateIfNotExistsAsync();
            var entity = await _tableClient.GetEntityIfExistsAsync<TableEntity>(userId, rowKey);
            return entity.HasValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existence for user {UserId}, rowKey {RowKey}", userId, rowKey);
            return false;
        }
    }
    private static GameResult MapFromTableEntity(TableEntity entity)
    {
        return new GameResult
        {
            PartitionKey = entity.PartitionKey,
            RowKey = entity.RowKey,
            Username = entity.GetString("Username") ?? "Unknown",
            NetWPM = entity.GetDouble("NetWPM") ?? 0,
            Accuracy = entity.GetDouble("Accuracy") ?? 0,
            GrossWPM = entity.GetDouble("GrossWPM") ?? 0,
            CompositeScore = entity.GetDouble("CompositeScore") ?? 0, // Add CompositeScore mapping
            ProblemKeysJson = entity.GetString("ProblemKeysJson") ?? "{}",
            GameTimestamp = entity.GetDateTime("GameTimestamp") ?? DateTime.UtcNow,
            Timestamp = entity.Timestamp,
            ETag = entity.ETag
        };
    }
}
