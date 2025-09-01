using Azure.Data.Tables;
using Azure;

namespace PoFastType.Shared.Models;

public class GameResult : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty; // UserId
    public string RowKey { get; set; } = string.Empty; // Reverse timestamp
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Username { get; set; } = string.Empty;
    public double NetWPM { get; set; }
    public double Accuracy { get; set; }
    public double GrossWPM { get; set; }
    public double CompositeScore { get; set; } // New composite scoring system for leaderboard
    public string ProblemKeysJson { get; set; } = string.Empty;

    // Additional property for custom timestamp
    public DateTime GameTimestamp { get; set; }
}