using PoFastType.Shared.Models;

namespace PoFastType.Api.Repositories;

/// <summary>
/// Repository interface for keystroke data operations
/// Follows Repository Pattern and Single Responsibility Principle
/// </summary>
public interface IKeystrokeRepository
{
    /// <summary>
    /// Add a single keystroke record
    /// </summary>
    Task<KeystrokeData> AddKeystrokeAsync(KeystrokeData keystroke);

    /// <summary>
    /// Add multiple keystroke records in batch (more efficient)
    /// </summary>
    Task AddKeystrokesBatchAsync(IEnumerable<KeystrokeData> keystrokes);

    /// <summary>
    /// Get all keystrokes for a specific user
    /// </summary>
    Task<IEnumerable<KeystrokeData>> GetUserKeystrokesAsync(string userId, int? limit = null);

    /// <summary>
    /// Get keystrokes for a specific game session
    /// </summary>
    Task<IEnumerable<KeystrokeData>> GetGameKeystrokesAsync(string userId, string gameId);

    /// <summary>
    /// Get keystrokes for a user within a date range
    /// </summary>
    Task<IEnumerable<KeystrokeData>> GetUserKeystrokesInRangeAsync(string userId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Delete all keystrokes for a specific user (privacy/GDPR compliance)
    /// </summary>
    Task DeleteUserKeystrokesAsync(string userId);

    /// <summary>
    /// Get total keystroke count for a user
    /// </summary>
    Task<int> GetKeystrokeCountAsync(string userId);
}
