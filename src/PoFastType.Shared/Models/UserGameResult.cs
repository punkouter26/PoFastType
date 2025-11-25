namespace PoFastType.Shared.Models;

/// <summary>
/// Represents a user's typing test result with all metrics.
/// </summary>
public class UserGameResult
{
    public double NetWPM { get; set; }
    public double Accuracy { get; set; }
    public double GrossWPM { get; set; }
    public string ProblemKeysJson { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
