namespace PoFastType.Shared.Models;

public class LeaderboardEntry
{
    public int Rank { get; set; }
    public string Username { get; set; } = string.Empty;
    public double NetWPM { get; set; }
    public double CompositeScore { get; set; } // Primary score for leaderboard ranking
    public DateTime Timestamp { get; set; }
}
