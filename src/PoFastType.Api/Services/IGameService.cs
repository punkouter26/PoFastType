using PoFastType.Shared.Models;

namespace PoFastType.Api.Services;

/// <summary>
/// Service Layer Pattern - Encapsulates business logic for game operations
/// Single Responsibility Principle (SOLID) - Handles only game-related business logic
/// </summary>
public interface IGameService
{
    /// <summary>
    /// Submits a completed game result to the system.
    /// Calculates composite score and stores in the repository.
    /// </summary>
    /// <param name="gameResult">The game result data to submit</param>
    /// <param name="userId">The ID of the user who completed the game</param>
    /// <param name="username">The display name of the user</param>
    /// <returns>The stored game result with calculated scores</returns>
    Task<GameResult> SubmitGameResultAsync(GameResult gameResult, string userId, string username);

    /// <summary>
    /// Retrieves all game results for a specific user.
    /// </summary>
    /// <param name="userId">The user ID to query</param>
    /// <returns>Collection of game results ordered by most recent first</returns>
    Task<IEnumerable<GameResult>> GetUserStatsAsync(string userId);

    /// <summary>
    /// Retrieves the top scores for the global leaderboard.
    /// </summary>
    /// <param name="topCount">Number of top scores to retrieve (default: 10)</param>
    /// <returns>Collection of leaderboard entries ranked by composite score</returns>
    Task<IEnumerable<LeaderboardEntry>> GetLeaderboardAsync(int topCount = 10);
}
