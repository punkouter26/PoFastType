namespace PoFastType.Shared.Models;

public class UserProfile
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
    public int TotalTestsCompleted { get; set; }
    public double BestNetWPM { get; set; }
    public double AverageNetWPM { get; set; }
    public double AverageAccuracy { get; set; }
    public TimeSpan TotalTypingTime { get; set; }
    public Dictionary<string, int> ProblemKeys { get; set; } = new();
} 