namespace PoFastType.Shared.Models;

/// <summary>
/// Comprehensive biometric statistics for a user's typing patterns
/// </summary>
public class BiometricStats
{
    /// <summary>
    /// User identifier
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Total number of keystrokes recorded
    /// </summary>
    public int TotalKeystrokes { get; set; }

    /// <summary>
    /// Total number of games analyzed
    /// </summary>
    public int GamesAnalyzed { get; set; }

    /// <summary>
    /// Metrics for each individual key
    /// </summary>
    public List<KeyMetrics> KeyboardHeatmap { get; set; } = new();

    /// <summary>
    /// Average typing rhythm (consistency in milliseconds between keystrokes)
    /// Lower is more consistent
    /// </summary>
    public double TypingRhythmVariance { get; set; }

    /// <summary>
    /// Most commonly mistyped keys
    /// </summary>
    public List<string> ProblemKeys { get; set; } = new();

    /// <summary>
    /// Most accurate keys
    /// </summary>
    public List<string> StrongKeys { get; set; } = new();

    /// <summary>
    /// Average time between keystrokes (milliseconds)
    /// </summary>
    public double AverageKeystrokeInterval { get; set; }

    /// <summary>
    /// Error patterns detected (e.g., "commonly confuses 'e' with 'r'")
    /// </summary>
    public List<ErrorPattern> ErrorPatterns { get; set; } = new();

    /// <summary>
    /// Typing speed degradation over time (percentage)
    /// Negative value indicates improvement during session
    /// </summary>
    public double FatigueIndex { get; set; }

    /// <summary>
    /// Peak typing speed achieved (WPM)
    /// </summary>
    public double PeakWPM { get; set; }

    /// <summary>
    /// Average typing speed across all sessions (WPM)
    /// </summary>
    public double AverageWPM { get; set; }

    /// <summary>
    /// Overall typing accuracy (percentage)
    /// </summary>
    public double OverallAccuracy { get; set; }

    /// <summary>
    /// When these statistics were last calculated
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a detected error pattern in typing behavior
/// </summary>
public class ErrorPattern
{
    /// <summary>
    /// The key that was expected
    /// </summary>
    public string ExpectedKey { get; set; } = string.Empty;

    /// <summary>
    /// The key that was actually pressed
    /// </summary>
    public string ActualKey { get; set; } = string.Empty;

    /// <summary>
    /// Number of times this error occurred
    /// </summary>
    public int Occurrences { get; set; }

    /// <summary>
    /// Percentage of times expected key resulted in this error
    /// </summary>
    public double ErrorRate { get; set; }
}
