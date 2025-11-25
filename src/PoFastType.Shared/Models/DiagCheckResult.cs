namespace PoFastType.Shared.Models;

/// <summary>
/// Represents the result of a single diagnostic check.
/// </summary>
public class DiagCheckResult
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
