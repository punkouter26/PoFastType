namespace PoFastType.Api.Services.HealthChecks;

/// <summary>
/// Health check strategy for verifying internet connectivity.
/// </summary>
public class InternetConnectivityHealthCheck : IHealthCheckStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<InternetConnectivityHealthCheck> _logger;

    public string CheckName => "Internet Connectivity";

    public InternetConnectivityHealthCheck(IHttpClientFactory httpClientFactory, ILogger<InternetConnectivityHealthCheck> logger)
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
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            var response = await httpClient.GetAsync("https://www.msftconnecttest.com/connecttest.txt");

            if (response.IsSuccessStatusCode)
            {
                result.Status = "OK";
                result.Message = "Online";
                _logger.LogInformation("[Diag] {CheckName} check OK", CheckName);
            }
            else
            {
                result.Status = "Error";
                result.Message = $"HTTP {response.StatusCode}";
                _logger.LogWarning("[Diag] {CheckName} check failed with status {StatusCode}", CheckName, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            result.Status = "Error";
            result.Message = "No internet connection detected.";
            _logger.LogError(ex, "[Diag] {CheckName} check failed", CheckName);
        }

        return result;
    }
}
