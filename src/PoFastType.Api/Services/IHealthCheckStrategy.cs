namespace PoFastType.Api.Services;

/// <summary>
/// Strategy interface for performing individual health checks.
/// Follows the Strategy Pattern to reduce cyclomatic complexity in DiagController.
/// </summary>
public interface IHealthCheckStrategy
{
    /// <summary>
    /// Gets the name of the health check (e.g., "Internet Connectivity", "Azure Table Storage").
    /// </summary>
    string CheckName { get; }

    /// <summary>
    /// Executes the health check and returns the result.
    /// </summary>
    /// <returns>A <see cref="DiagCheckResult"/> containing the check status, name, and message.</returns>
    Task<DiagCheckResult> ExecuteAsync();
}

/// <summary>
/// Result of a diagnostic health check.
/// </summary>
public class DiagCheckResult
{
    /// <summary>
    /// Name of the health check (e.g., "Internet Connectivity").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Status of the check: "OK", "Warning", or "Error".
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Detailed message about the check result.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response from the diagnostic health endpoint.
/// </summary>
public class DiagHealthResponse
{
    /// <summary>
    /// Overall status: "OK" or "Issues detected".
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// List of individual health check results.
    /// </summary>
    public List<DiagCheckResult> Checks { get; set; } = new();

    /// <summary>
    /// UTC timestamp of when the health check was performed.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
