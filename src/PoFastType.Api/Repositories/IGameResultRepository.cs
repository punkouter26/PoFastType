using PoFastType.Shared.Models;

namespace PoFastType.Api.Repositories;

/// <summary>
/// Repository Pattern (GoF) - Encapsulates data access logic and provides a uniform interface for accessing domain objects
/// Single Responsibility Principle (SOLID) - Handles only game result data operations
/// </summary>
public interface IGameResultRepository
{
    /// <summary>
    /// Adds a new game result to Azure Table Storage.
    /// </summary>
    /// <param name="gameResult">The game result to store</param>
    /// <returns>The stored game result with Azure-managed properties populated</returns>
    Task<GameResult> AddAsync(GameResult gameResult);

    /// <summary>
    /// Retrieves all game results for a specific user.
    /// </summary>
    /// <param name="userId">The user ID to query (used as PartitionKey)</param>
    /// <returns>Collection of game results ordered by most recent first</returns>
    Task<IEnumerable<GameResult>> GetUserResultsAsync(string userId);

    /// <summary>
    /// Retrieves the top game results across all users for the leaderboard.
    /// </summary>
    /// <param name="count">Number of top results to retrieve</param>
    /// <returns>Collection of top game results ordered by composite score descending</returns>
    Task<IEnumerable<GameResult>> GetTopResultsAsync(int count);

    /// <summary>
    /// Checks if a game result already exists in storage.
    /// </summary>
    /// <param name="userId">The user ID (PartitionKey)</param>
    /// <param name="rowKey">The row key (typically reverse timestamp)</param>
    /// <returns>True if the result exists, false otherwise</returns>
    Task<bool> ExistsAsync(string userId, string rowKey);
}
