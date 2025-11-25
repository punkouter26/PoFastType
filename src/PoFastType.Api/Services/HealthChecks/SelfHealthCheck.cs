namespace PoFastType.Api.Services.HealthChecks;

/// <summary>
/// Health check strategy for basic application self-check.
/// </summary>
public class SelfHealthCheck : IHealthCheckStrategy
{
    private readonly ILogger<SelfHealthCheck> _logger;

    public string CheckName => "Health Check";

    public SelfHealthCheck(ILogger<SelfHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<DiagCheckResult> ExecuteAsync()
    {
        var result = new DiagCheckResult
        {
            Name = CheckName,
            Status = "OK",
            Message = "Application is running and responding."
        };

        _logger.LogInformation("[Diag] {CheckName} OK", CheckName);

        return Task.FromResult(result);
    }
}
