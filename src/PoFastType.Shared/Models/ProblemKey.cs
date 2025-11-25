namespace PoFastType.Shared.Models;

/// <summary>
/// Represents a problem key with frequency of errors
/// </summary>
public class ProblemKey
{
    public string IntendedKey { get; set; } = string.Empty;
    public string ActualKey { get; set; } = string.Empty;
    public int Frequency { get; set; }
}
