namespace PoFastType.Shared.Models;

/// <summary>
/// Represents a single health check entry in the health check response.
/// </summary>
public class HealthCheckEntry
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Duration { get; set; } = string.Empty;
    public string? Exception { get; set; }
}
