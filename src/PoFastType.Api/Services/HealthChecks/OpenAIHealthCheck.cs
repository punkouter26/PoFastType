namespace PoFastType.Api.Services.HealthChecks;

/// <summary>
/// Health check strategy for verifying Azure OpenAI service connectivity.
/// </summary>
public class OpenAIHealthCheck : IHealthCheckStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAIHealthCheck> _logger;

    public string CheckName => "Azure OpenAI";

    public OpenAIHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<OpenAIHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<DiagCheckResult> ExecuteAsync()
    {
        var result = new DiagCheckResult { Name = CheckName };

        try
        {
            var endpoint = _configuration["AzureOpenAI:Endpoint"];

            if (!string.IsNullOrEmpty(endpoint))
            {
                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                var response = await httpClient.GetAsync(endpoint);

                result.Status = "OK";
                result.Message = "Azure OpenAI endpoint is reachable.";
                _logger.LogInformation("[Diag] {CheckName} check OK", CheckName);
            }
            else
            {
                result.Status = "Warning";
                result.Message = "Azure OpenAI endpoint not configured.";
                _logger.LogWarning("[Diag] {CheckName} endpoint not configured", CheckName);
            }
        }
        catch (Exception ex)
        {
            result.Status = "Error";
            result.Message = "Cannot reach Azure OpenAI service";
            _logger.LogError(ex, "[Diag] {CheckName} check failed", CheckName);
        }

        return result;
    }
}
