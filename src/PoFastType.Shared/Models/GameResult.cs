using Azure.Data.Tables;
using Azure;

namespace PoFastType.Shared.Models;

/// <summary>
/// Represents the result of a completed typing game session.
/// Stored in Azure Table Storage for leaderboard and user statistics.
/// </summary>
public class GameResult : ITableEntity
{
    /// <summary>
    /// Partition Key: User ID for efficient querying by user
    /// </summary>
    public string PartitionKey { get; set; } = string.Empty;

    /// <summary>
    /// Row Key: Reverse timestamp (for descending order) to get latest games first
    /// </summary>
    public string RowKey { get; set; } = string.Empty;

    /// <summary>
    /// Azure Table Storage timestamp (automatically managed)
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Azure Table Storage ETag for optimistic concurrency
    /// </summary>
    public ETag ETag { get; set; }

    /// <summary>
    /// Display name of the user who completed this game
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Net Words Per Minute (WPM) - adjusted for errors
    /// Calculated as: (TotalCharacters / 5 - Errors) / (Time in minutes)
    /// </summary>
    public double NetWPM { get; set; }

    /// <summary>
    /// Typing accuracy as a percentage (0-100)
    /// </summary>
    public double Accuracy { get; set; }

    /// <summary>
    /// Gross Words Per Minute (WPM) - not adjusted for errors
    /// Calculated as: (TotalCharacters / 5) / (Time in minutes)
    /// </summary>
    public double GrossWPM { get; set; }

    /// <summary>
    /// Composite score combining speed and accuracy for leaderboard ranking
    /// Formula: NetWPM * (Accuracy / 100)
    /// </summary>
    public double CompositeScore { get; set; }

    /// <summary>
    /// JSON-serialized array of keys that caused errors during the game
    /// </summary>
    public string ProblemKeysJson { get; set; } = string.Empty;

    /// <summary>
    /// When the game was completed (UTC)
    /// </summary>
    public DateTime GameTimestamp { get; set; }
}