namespace PoFastType.Api.Services.HealthChecks;

/// <summary>
/// Health check strategy for verifying Azure service connectivity.
/// </summary>
public class AzureConnectivityHealthCheck : IHealthCheckStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AzureConnectivityHealthCheck> _logger;

    public string CheckName => "Azure Connectivity";

    public AzureConnectivityHealthCheck(IHttpClientFactory httpClientFactory, ILogger<AzureConnectivityHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<DiagCheckResult> ExecuteAsync()
    {
        var result = new DiagCheckResult { Name = CheckName };

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            var response = await httpClient.GetAsync("https://management.azure.com/");

            result.Status = response.IsSuccessStatusCode ? "OK" : "Warning";
            result.Message = response.IsSuccessStatusCode ? "Azure reachable" : $"Azure returned {response.StatusCode}";
            _logger.LogInformation("[Diag] {CheckName} check: {Status}", CheckName, result.Status);
        }
        catch (Exception ex)
        {
            result.Status = "Error";
            result.Message = "Cannot reach Azure services";
            _logger.LogError(ex, "[Diag] {CheckName} check failed", CheckName);
        }

        return result;
    }
}
