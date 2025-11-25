namespace PoFastType.Shared.Models;

/// <summary>
/// Represents a health check response from the diagnostics endpoint.
/// </summary>
public class DiagHealthResponse
{
    public string Status { get; set; } = string.Empty;
    public List<DiagCheckResult> Checks { get; set; } = new();
    public DateTime Timestamp { get; set; }
}
