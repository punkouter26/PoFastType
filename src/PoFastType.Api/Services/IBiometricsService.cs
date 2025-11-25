using PoFastType.Shared.Models;

namespace PoFastType.Api.Services;

/// <summary>
/// Service interface for biometric analysis operations
/// Follows Single Responsibility Principle - handles only keystroke analytics
/// </summary>
public interface IBiometricsService
{
    /// <summary>
    /// Calculate comprehensive biometric statistics for a user
    /// </summary>
    Task<BiometricStats> CalculateUserBiometricsAsync(string userId);

    /// <summary>
    /// Calculate biometric statistics for a specific game session
    /// </summary>
    Task<BiometricStats> CalculateGameBiometricsAsync(string userId, string gameId);

    /// <summary>
    /// Get keyboard heatmap data for visualization
    /// </summary>
    Task<List<KeyMetrics>> GetKeyboardHeatmapAsync(string userId);

    /// <summary>
    /// Identify problem keys (low accuracy or high error rate)
    /// </summary>
    Task<List<string>> GetProblemKeysAsync(string userId, int topCount = 10);

    /// <summary>
    /// Identify strong keys (high accuracy and speed)
    /// </summary>
    Task<List<string>> GetStrongKeysAsync(string userId, int topCount = 10);

    /// <summary>
    /// Detect error patterns (commonly confused keys)
    /// </summary>
    Task<List<ErrorPattern>> DetectErrorPatternsAsync(string userId);

    /// <summary>
    /// Calculate typing fatigue index (speed degradation over time)
    /// </summary>
    Task<double> CalculateFatigueIndexAsync(string userId, string gameId);
}
