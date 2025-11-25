namespace PoFastType.Shared.Models;

/// <summary>
/// Represents a keystroke error during typing
/// </summary>
public class KeystrokeError
{
    public string IntendedKey { get; set; } = string.Empty;
    public string ActualKey { get; set; } = string.Empty;
    public int Position { get; set; }
}
