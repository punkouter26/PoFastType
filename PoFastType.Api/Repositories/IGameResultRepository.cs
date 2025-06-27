using PoFastType.Shared.Models;

namespace PoFastType.Api.Repositories;

/// <summary>
/// Repository Pattern (GoF) - Encapsulates data access logic and provides a uniform interface for accessing domain objects
/// Single Responsibility Principle (SOLID) - Handles only game result data operations
/// </summary>
public interface IGameResultRepository
{
    Task<GameResult> AddAsync(GameResult gameResult);
    Task<IEnumerable<GameResult>> GetUserResultsAsync(string userId);
    Task<IEnumerable<GameResult>> GetTopResultsAsync(int count);
    Task<bool> ExistsAsync(string userId, string rowKey);
}
