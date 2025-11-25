using PoFastType.Api.Repositories;
using PoFastType.Shared.Models;

namespace PoFastType.Api.Services;

/// <summary>
/// Service Layer Pattern - Encapsulates business logic for game operations
/// Single Responsibility Principle (SOLID) - Handles only game-related business logic
/// Dependency Inversion Principle (SOLID) - Depends on IGameResultRepository abstraction
/// </summary>
public class GameService : IGameService
{
    private readonly IGameResultRepository _gameResultRepository;
    private readonly ILogger<GameService> _logger;

    public GameService(IGameResultRepository gameResultRepository, ILogger<GameService> logger)
    {
        _gameResultRepository = gameResultRepository ?? throw new ArgumentNullException(nameof(gameResultRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GameResult> SubmitGameResultAsync(GameResult gameResult, string userId, string username)
    {
        // Validate input parameters
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        if (string.IsNullOrEmpty(username))
            throw new ArgumentException("Username cannot be null or empty", nameof(username));

        if (gameResult == null)
            throw new ArgumentNullException(nameof(gameResult));

        // Business rule: Validate WPM and accuracy ranges
        if (gameResult.NetWPM < 0 || gameResult.NetWPM > 300)
            throw new ArgumentException("Net WPM must be between 0 and 300", nameof(gameResult));

        if (gameResult.Accuracy < 0 || gameResult.Accuracy > 100)
            throw new ArgumentException("Accuracy must be between 0 and 100", nameof(gameResult));

        try
        {
            // Set required fields
            gameResult.PartitionKey = userId;
            gameResult.RowKey = GetReverseTimestamp();
            gameResult.Username = username;
            gameResult.Timestamp = DateTime.UtcNow;

            var result = await _gameResultRepository.AddAsync(gameResult);

            _logger.LogInformation("Game result submitted successfully for user {UserId}", userId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit game result for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<GameResult>> GetUserStatsAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        try
        {
            return await _gameResultRepository.GetUserResultsAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user stats for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<LeaderboardEntry>> GetLeaderboardAsync(int topCount = 10)
    {
        if (topCount <= 0 || topCount > 100)
            throw new ArgumentException("Top count must be between 1 and 100", nameof(topCount));

        try
        {
            var topResults = await _gameResultRepository.GetTopResultsAsync(topCount);
            return topResults.Select((result, index) => new LeaderboardEntry
            {
                Rank = index + 1,
                Username = result.Username,
                GrossWPM = result.GrossWPM,
                Accuracy = result.Accuracy,
                NetWPM = result.NetWPM,
                CompositeScore = result.CompositeScore,
                Timestamp = result.GameTimestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve leaderboard");
            throw;
        }
    }

    private static string GetReverseTimestamp()
    {
        // Create a reverse timestamp for RowKey to ensure newest entries come first
        return (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("D20");
    }
}
