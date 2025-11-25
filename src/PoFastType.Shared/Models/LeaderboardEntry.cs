namespace PoFastType.Shared.Models;

/// <summary>
/// Represents a single entry in the typing speed leaderboard.
/// Used for displaying top scores and rankings.
/// </summary>
public class LeaderboardEntry
{
    /// <summary>
    /// Position in the leaderboard (1 = first place)
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Display name of the user
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gross Words Per Minute (WPM) - raw typing speed
    /// </summary>
    public double GrossWPM { get; set; }

    /// <summary>
    /// Typing accuracy percentage
    /// </summary>
    public double Accuracy { get; set; }

    /// <summary>
    /// Net Words Per Minute (WPM) - typing speed adjusted for errors
    /// </summary>
    public double NetWPM { get; set; }

    /// <summary>
    /// Composite score used for ranking (NetWPM * Accuracy%)
    /// Higher is better
    /// </summary>
    public double CompositeScore { get; set; }

    /// <summary>
    /// When this result was achieved (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; }
}
