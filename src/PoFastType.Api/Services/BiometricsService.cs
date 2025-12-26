using PoFastType.Api.Repositories;
using PoFastType.Shared.Models;

namespace PoFastType.Api.Services;

/// <summary>
/// Service for calculating biometric statistics from keystroke data
/// Follows Single Responsibility Principle - handles only analytics calculations
/// </summary>
public class BiometricsService : IBiometricsService
{
    private readonly IKeystrokeRepository _keystrokeRepository;
    private readonly ILogger<BiometricsService> _logger;

    public BiometricsService(
        IKeystrokeRepository keystrokeRepository,
        ILogger<BiometricsService> logger)
    {
        ArgumentNullException.ThrowIfNull(keystrokeRepository);
        ArgumentNullException.ThrowIfNull(logger);
        
        _keystrokeRepository = keystrokeRepository;
        _logger = logger;
    }

    public async Task<BiometricStats> CalculateUserBiometricsAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        try
        {
            var keystrokes = (await _keystrokeRepository.GetUserKeystrokesAsync(userId)).ToList();

            if (!keystrokes.Any())
            {
                _logger.LogInformation("No keystroke data found for user {UserId}", userId);
                return new BiometricStats { UserId = userId };
            }

            var stats = new BiometricStats
            {
                UserId = userId,
                TotalKeystrokes = keystrokes.Count,
                GamesAnalyzed = keystrokes.Select(k => k.GameId).Distinct().Count(),
                KeyboardHeatmap = await CalculateKeyMetricsAsync(keystrokes),
                TypingRhythmVariance = CalculateRhythmVariance(keystrokes),
                AverageKeystrokeInterval = keystrokes.Any() ? keystrokes.Average(k => k.IntervalMs) : 0,
                ErrorPatterns = CalculateErrorPatterns(keystrokes),
                PeakWPM = keystrokes.Any() ? keystrokes.Max(k => k.CurrentWPM) : 0,
                AverageWPM = keystrokes.Any() ? keystrokes.Average(k => k.CurrentWPM) : 0,
                OverallAccuracy = keystrokes.Any() ? keystrokes.Average(k => k.CurrentAccuracy) : 0,
                LastUpdated = DateTime.UtcNow
            };

            // Identify problem and strong keys
            stats.ProblemKeys = stats.KeyboardHeatmap
                .Where(k => k.Accuracy < 85)
                .OrderBy(k => k.Accuracy)
                .Take(10)
                .Select(k => k.Key)
                .ToList();

            stats.StrongKeys = stats.KeyboardHeatmap
                .Where(k => k.Accuracy > 95 && k.TotalPresses > 5)
                .OrderByDescending(k => k.Accuracy)
                .ThenBy(k => k.AverageIntervalMs)
                .Take(10)
                .Select(k => k.Key)
                .ToList();

            // Calculate fatigue across all games
            stats.FatigueIndex = await CalculateOverallFatigueAsync(userId, keystrokes);

            _logger.LogInformation("Calculated biometrics for user {UserId}: {Total} keystrokes, {Games} games",
                userId, stats.TotalKeystrokes, stats.GamesAnalyzed);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate biometrics for user {UserId}", userId);
            throw;
        }
    }

    public async Task<BiometricStats> CalculateGameBiometricsAsync(string userId, string gameId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(gameId))
            throw new ArgumentException("Game ID cannot be null or empty", nameof(gameId));

        try
        {
            var keystrokes = (await _keystrokeRepository.GetGameKeystrokesAsync(userId, gameId)).ToList();

            if (!keystrokes.Any())
            {
                _logger.LogInformation("No keystroke data found for game {GameId}", gameId);
                return new BiometricStats { UserId = userId };
            }

            var stats = new BiometricStats
            {
                UserId = userId,
                TotalKeystrokes = keystrokes.Count,
                GamesAnalyzed = 1,
                KeyboardHeatmap = await CalculateKeyMetricsAsync(keystrokes),
                TypingRhythmVariance = CalculateRhythmVariance(keystrokes),
                AverageKeystrokeInterval = keystrokes.Average(k => k.IntervalMs),
                ErrorPatterns = CalculateErrorPatterns(keystrokes),
                PeakWPM = keystrokes.Max(k => k.CurrentWPM),
                AverageWPM = keystrokes.Average(k => k.CurrentWPM),
                OverallAccuracy = keystrokes.Average(k => k.CurrentAccuracy),
                FatigueIndex = await CalculateFatigueIndexAsync(userId, gameId),
                LastUpdated = DateTime.UtcNow
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate biometrics for game {GameId}", gameId);
            throw;
        }
    }

    public async Task<List<KeyMetrics>> GetKeyboardHeatmapAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        try
        {
            var keystrokes = (await _keystrokeRepository.GetUserKeystrokesAsync(userId)).ToList();
            return await CalculateKeyMetricsAsync(keystrokes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get keyboard heatmap for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<string>> GetProblemKeysAsync(string userId, int topCount = 10)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        try
        {
            var heatmap = await GetKeyboardHeatmapAsync(userId);
            return heatmap
                .Where(k => k.Accuracy < 85 && k.TotalPresses > 3)
                .OrderBy(k => k.Accuracy)
                .Take(topCount)
                .Select(k => k.Key)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get problem keys for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<string>> GetStrongKeysAsync(string userId, int topCount = 10)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        try
        {
            var heatmap = await GetKeyboardHeatmapAsync(userId);
            return heatmap
                .Where(k => k.Accuracy > 95 && k.TotalPresses > 5)
                .OrderByDescending(k => k.Accuracy)
                .ThenBy(k => k.AverageIntervalMs)
                .Take(topCount)
                .Select(k => k.Key)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get strong keys for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<ErrorPattern>> DetectErrorPatternsAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        try
        {
            var keystrokes = (await _keystrokeRepository.GetUserKeystrokesAsync(userId)).ToList();
            return CalculateErrorPatterns(keystrokes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect error patterns for user {UserId}", userId);
            throw;
        }
    }

    public async Task<double> CalculateFatigueIndexAsync(string userId, string gameId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(gameId))
            throw new ArgumentException("Game ID cannot be null or empty", nameof(gameId));

        try
        {
            var keystrokes = (await _keystrokeRepository.GetGameKeystrokesAsync(userId, gameId))
                .OrderBy(k => k.SequenceNumber)
                .ToList();

            if (keystrokes.Count < 20)
                return 0; // Not enough data to calculate fatigue

            // Split into first and last quarters
            var quarterSize = keystrokes.Count / 4;
            var firstQuarter = keystrokes.Take(quarterSize).ToList();
            var lastQuarter = keystrokes.Skip(keystrokes.Count - quarterSize).ToList();

            var firstQuarterWPM = firstQuarter.Any() ? firstQuarter.Average(k => k.CurrentWPM) : 0;
            var lastQuarterWPM = lastQuarter.Any() ? lastQuarter.Average(k => k.CurrentWPM) : 0;

            if (firstQuarterWPM == 0)
                return 0;

            // Positive value = speed decreased (fatigued), negative = speed increased (warmed up)
            var fatigueIndex = ((firstQuarterWPM - lastQuarterWPM) / firstQuarterWPM) * 100;

            return Math.Round(fatigueIndex, 2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate fatigue index for game {GameId}", gameId);
            throw;
        }
    }

    // Private helper methods

    private async Task<List<KeyMetrics>> CalculateKeyMetricsAsync(List<KeystrokeData> keystrokes)
    {
        if (!keystrokes.Any())
            return new List<KeyMetrics>();

        var keyGroups = keystrokes
            .Where(k => !k.IsBackspace && !string.IsNullOrWhiteSpace(k.Key))
            .GroupBy(k => k.Key);

        var metrics = new List<KeyMetrics>();

        foreach (var group in keyGroups)
        {
            var keyData = group.ToList();
            var correctCount = keyData.Count(k => k.IsCorrect);
            var totalCount = keyData.Count;
            var accuracy = totalCount > 0 ? (double)correctCount / totalCount * 100 : 0;

            var intervals = keyData.Where(k => k.IntervalMs > 0).Select(k => (double)k.IntervalMs).ToList();

            var metric = new KeyMetrics
            {
                Key = group.Key,
                TotalPresses = totalCount,
                CorrectPresses = correctCount,
                IncorrectPresses = totalCount - correctCount,
                Accuracy = Math.Round(accuracy, 2),
                AverageIntervalMs = intervals.Any() ? Math.Round(intervals.Average(), 2) : 0,
                FastestIntervalMs = intervals.Any() ? intervals.Min() : 0,
                SlowestIntervalMs = intervals.Any() ? intervals.Max() : 0
            };

            // Calculate heat level (0.0 to 1.0) - higher for problematic keys
            // Factors: low accuracy (weight: 0.7), high average interval (weight: 0.3)
            var accuracyScore = (100 - metric.Accuracy) / 100; // Inverted: lower accuracy = higher heat
            var speedScore = intervals.Any() ? Math.Min(metric.AverageIntervalMs / 500, 1.0) : 0; // Normalize to 500ms max
            metric.HeatLevel = Math.Round((accuracyScore * 0.7) + (speedScore * 0.3), 3);

            metrics.Add(metric);
        }

        return metrics.OrderByDescending(m => m.TotalPresses).ToList();
    }

    private double CalculateRhythmVariance(List<KeystrokeData> keystrokes)
    {
        var intervals = keystrokes
            .Where(k => k.IntervalMs > 0 && k.IntervalMs < 2000) // Exclude outliers
            .Select(k => (double)k.IntervalMs)
            .ToList();

        if (intervals.Count < 2)
            return 0;

        var mean = intervals.Average();
        var sumSquaredDifferences = intervals.Sum(interval => Math.Pow(interval - mean, 2));
        var variance = sumSquaredDifferences / intervals.Count;

        return Math.Round(variance, 2);
    }

    private List<ErrorPattern> CalculateErrorPatterns(List<KeystrokeData> keystrokes)
    {
        var incorrectKeystrokes = keystrokes
            .Where(k => !k.IsCorrect && !k.IsBackspace && !string.IsNullOrWhiteSpace(k.Key))
            .ToList();

        if (!incorrectKeystrokes.Any())
            return new List<ErrorPattern>();

        var patterns = incorrectKeystrokes
            .GroupBy(k => new { k.ExpectedChar, k.Key })
            .Where(g => g.Count() > 2) // Only patterns that occurred more than twice
            .Select(g =>
            {
                var expectedKey = g.Key.ExpectedChar;
                var actualKey = g.Key.Key;
                var occurrences = g.Count();

                // Calculate error rate (how often expected key resulted in this error)
                var totalExpectedOccurrences = keystrokes.Count(k => k.ExpectedChar == expectedKey);
                var errorRate = totalExpectedOccurrences > 0
                    ? (double)occurrences / totalExpectedOccurrences * 100
                    : 0;

                return new ErrorPattern
                {
                    ExpectedKey = expectedKey,
                    ActualKey = actualKey,
                    Occurrences = occurrences,
                    ErrorRate = Math.Round(errorRate, 2)
                };
            })
            .OrderByDescending(p => p.Occurrences)
            .Take(20)
            .ToList();

        return patterns;
    }

    private async Task<double> CalculateOverallFatigueAsync(string userId, List<KeystrokeData> allKeystrokes)
    {
        // Calculate average fatigue across all games
        var gameIds = allKeystrokes.Select(k => k.GameId).Distinct().ToList();

        if (gameIds.Count == 0)
            return 0;

        var fatigueIndices = new List<double>();

        foreach (var gameId in gameIds)
        {
            try
            {
                var fatigue = await CalculateFatigueIndexAsync(userId, gameId);
                fatigueIndices.Add(fatigue);
            }
            catch (ArgumentException ex)
            {
                // Skip games with invalid arguments (e.g., null/empty IDs)
                _logger.LogDebug(ex, "Skipping game {GameId} due to invalid arguments", gameId);
                continue;
            }
            catch (Exception ex)
            {
                // Log unexpected errors but continue processing other games
                _logger.LogWarning(ex, "Failed to calculate fatigue for game {GameId}, skipping", gameId);
                continue;
            }
        }

        return fatigueIndices.Any() ? Math.Round(fatigueIndices.Average(), 2) : 0;
    }
}
