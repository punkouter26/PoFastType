using Microsoft.AspNetCore.Mvc;
using PoFastType.Api.Services;

namespace PoFastType.Api.Controllers;

/// <summary>
/// Diagnostic controller for system health monitoring.
/// Refactored using Strategy Pattern to reduce cyclomatic complexity.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DiagController : ControllerBase
{
    private readonly IEnumerable<IHealthCheckStrategy> _healthCheckStrategies;
    private readonly ILogger<DiagController> _logger;

    public DiagController(
        IEnumerable<IHealthCheckStrategy> healthCheckStrategies,
        ILogger<DiagController> logger)
    {
        _healthCheckStrategies = healthCheckStrategies;
        _logger = logger;
    }

    /// <summary>
    /// Performs comprehensive diagnostic health checks across all system components.
    /// </summary>
    /// <returns>Health check results for all system components.</returns>
    [HttpGet("health")]
    [HttpGet("/api/health")] // RESTful alias for better discoverability
    public async Task<IActionResult> Health()
    {
        _logger.LogInformation("[Diag] Starting comprehensive diagnostic checks");

        var results = new List<DiagCheckResult>();
        bool allOk = true;

        // Execute all health check strategies
        foreach (var strategy in _healthCheckStrategies)
        {
            var result = await strategy.ExecuteAsync();
            results.Add(result);

            if (result.Status == "Error")
            {
                allOk = false;
            }
        }

        _logger.LogInformation("[Diag] Diagnostic checks completed. Overall status: {Status}", allOk ? "OK" : "Issues detected");

        var response = new DiagHealthResponse
        {
            Status = allOk ? "OK" : "Issues detected",
            Timestamp = DateTime.UtcNow,
            Checks = results
        };

        return Ok(response);
    }
}