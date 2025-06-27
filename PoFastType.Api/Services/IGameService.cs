using PoFastType.Shared.Models;

namespace PoFastType.Api.Services;

/// <summary>
/// Service Layer Pattern - Encapsulates business logic for game operations
/// Single Responsibility Principle (SOLID) - Handles only game-related business logic
/// </summary>
public interface IGameService
{
    Task<GameResult> SubmitGameResultAsync(GameResult gameResult, string userId, string username);
    Task<IEnumerable<GameResult>> GetUserStatsAsync(string userId);
    Task<IEnumerable<LeaderboardEntry>> GetLeaderboardAsync(int topCount = 10);
}
