using Microsoft.AspNetCore.Components;

namespace PoFastType.Client.Services;

/// <summary>
/// Service to manage game state and notify components when scores are updated
/// Implements Observer Pattern (GoF) for component communication
/// </summary>
public class GameStateService
{
    /// <summary>
    /// Event fired when a new score is submitted successfully
    /// </summary>
    public event Action? ScoreSubmitted;

    /// <summary>
    /// Event fired when leaderboard should refresh
    /// </summary>
    public event Action? LeaderboardRefreshRequested;

    /// <summary>
    /// Event fired when user stats should refresh
    /// </summary>
    public event Action? UserStatsRefreshRequested;

    /// <summary>
    /// Notify all subscribed components that a score was submitted
    /// </summary>
    public void NotifyScoreSubmitted()
    {
        ScoreSubmitted?.Invoke();
        LeaderboardRefreshRequested?.Invoke();
        UserStatsRefreshRequested?.Invoke();
    }

    /// <summary>
    /// Request leaderboard refresh
    /// </summary>
    public void RequestLeaderboardRefresh()
    {
        LeaderboardRefreshRequested?.Invoke();
    }

    /// <summary>
    /// Request user stats refresh
    /// </summary>
    public void RequestUserStatsRefresh()
    {
        UserStatsRefreshRequested?.Invoke();
    }
}
