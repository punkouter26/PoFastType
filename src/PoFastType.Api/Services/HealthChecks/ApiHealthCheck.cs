namespace PoFastType.Api.Services.HealthChecks;

/// <summary>
/// Health check strategy for verifying the API itself is running.
/// </summary>
public class ApiHealthCheck : IHealthCheckStrategy
{
    private readonly ILogger<ApiHealthCheck> _logger;

    public string CheckName => "Backend API";

    public ApiHealthCheck(ILogger<ApiHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<DiagCheckResult> ExecuteAsync()
    {
        var result = new DiagCheckResult
        {
            Name = CheckName,
            Status = "OK",
            Message = "API is running and accessible."
        };

        _logger.LogInformation("[Diag] {CheckName} check OK", CheckName);

        return Task.FromResult(result);
    }
}
