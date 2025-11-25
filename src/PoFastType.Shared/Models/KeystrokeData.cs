using Azure;
using Azure.Data.Tables;

namespace PoFastType.Shared.Models;

/// <summary>
/// Represents a single keystroke event during a typing test.
/// Stored in Azure Table Storage for detailed biometric analysis.
/// </summary>
public class KeystrokeData : ITableEntity
{
    /// <summary>
    /// Partition Key: UserId (for efficient querying by user)
    /// </summary>
    public string PartitionKey { get; set; } = string.Empty;

    /// <summary>
    /// Row Key: GameId_SequenceNumber (e.g., "guid_0001", "guid_0002")
    /// Allows ordering keystrokes within a game
    /// </summary>
    public string RowKey { get; set; } = string.Empty;

    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    /// <summary>
    /// Unique identifier for the typing game session
    /// </summary>
    public string GameId { get; set; } = string.Empty;

    /// <summary>
    /// Sequential number of this keystroke in the game (0, 1, 2, ...)
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// The key that was pressed (e.g., "a", "Enter", "Backspace", "Shift")
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The expected character at this position in the text
    /// </summary>
    public string ExpectedChar { get; set; } = string.Empty;

    /// <summary>
    /// Whether the keystroke was correct (matches expected character)
    /// </summary>
    public bool IsCorrect { get; set; }

    /// <summary>
    /// Whether this was a backspace/deletion
    /// </summary>
    public bool IsBackspace { get; set; }

    /// <summary>
    /// Time elapsed since the start of the game (in milliseconds)
    /// </summary>
    public long ElapsedMs { get; set; }

    /// <summary>
    /// Time between this keystroke and the previous one (in milliseconds)
    /// </summary>
    public long IntervalMs { get; set; }

    /// <summary>
    /// Current position in the text (character index)
    /// </summary>
    public int TextPosition { get; set; }

    /// <summary>
    /// Current typing speed at this moment (WPM)
    /// </summary>
    public double CurrentWPM { get; set; }

    /// <summary>
    /// Current accuracy at this moment (percentage)
    /// </summary>
    public double CurrentAccuracy { get; set; }

    /// <summary>
    /// When this keystroke was recorded (UTC)
    /// </summary>
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
