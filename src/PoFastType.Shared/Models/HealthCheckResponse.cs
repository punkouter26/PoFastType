namespace PoFastType.Shared.Models;

/// <summary>
/// Represents a health check response from the API.
/// </summary>
public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public List<HealthCheckEntry> Checks { get; set; } = new();
    public string TotalDuration { get; set; } = string.Empty;
}
