namespace PoFastType.Shared.Models;

/// <summary>
/// Aggregated statistics for a specific key across all games
/// </summary>
public class KeyMetrics
{
    /// <summary>
    /// The keyboard key (e.g., "a", "Enter", "Space")
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Total number of times this key was pressed
    /// </summary>
    public int TotalPresses { get; set; }

    /// <summary>
    /// Number of correct presses
    /// </summary>
    public int CorrectPresses { get; set; }

    /// <summary>
    /// Number of incorrect presses
    /// </summary>
    public int IncorrectPresses { get; set; }

    /// <summary>
    /// Accuracy percentage for this key (0-100)
    /// </summary>
    public double Accuracy { get; set; }

    /// <summary>
    /// Average time to press this key (milliseconds)
    /// </summary>
    public double AverageIntervalMs { get; set; }

    /// <summary>
    /// Fastest time to press this key (milliseconds)
    /// </summary>
    public double FastestIntervalMs { get; set; }

    /// <summary>
    /// Slowest time to press this key (milliseconds)
    /// </summary>
    public double SlowestIntervalMs { get; set; }

    /// <summary>
    /// Heat level for visualization (0.0 to 1.0)
    /// Calculated based on accuracy and speed
    /// </summary>
    public double HeatLevel { get; set; }
}
