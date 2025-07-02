using Microsoft.AspNetCore.Mvc;
using Azure.Data.Tables;
using System.Net.NetworkInformation;

namespace PoFastType.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagController : ControllerBase
{
    private readonly TableClient _tableClient;
    private readonly ILogger<DiagController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public DiagController(IConfiguration configuration, ILogger<DiagController> logger, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        var connectionString = configuration["AzureTableStorage:ConnectionString"];
        var tableName = configuration["AzureTableStorage:TableName"];
        _tableClient = new TableClient(connectionString, tableName);
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        var results = new List<DiagCheckResult>();
        bool allOk = true;

        _logger.LogInformation("[Diag] Starting comprehensive diagnostic checks");        // 1. Internet connectivity check
        var internetStatus = new DiagCheckResult { Name = "Internet Connectivity" };
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            var internetResponse = await httpClient.GetAsync("https://www.msftconnecttest.com/connecttest.txt");
            if (internetResponse.IsSuccessStatusCode)
            {
                internetStatus.Status = "OK";
                internetStatus.Message = "Online";
                _logger.LogInformation("[Diag] Internet connectivity check OK");
            }
            else
            {
                internetStatus.Status = "Error";
                internetStatus.Message = $"HTTP {internetResponse.StatusCode}";
                allOk = false;
                _logger.LogWarning("[Diag] Internet connectivity check failed with status {StatusCode}", internetResponse.StatusCode);
            }
        }
        catch (Exception ex)
        {
            internetStatus.Status = "Error";
            internetStatus.Message = "No internet connection detected.";
            allOk = false;
            _logger.LogError(ex, "[Diag] Internet connectivity check failed");
        }
        results.Add(internetStatus);

        // 2. Azure connectivity check
        var azureStatus = new DiagCheckResult { Name = "Azure Connectivity" };
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            var azureResponse = await httpClient.GetAsync("https://management.azure.com/");
            azureStatus.Status = azureResponse.IsSuccessStatusCode ? "OK" : "Warning";
            azureStatus.Message = azureResponse.IsSuccessStatusCode ? "Azure reachable" : $"Azure returned {azureResponse.StatusCode}";
            _logger.LogInformation("[Diag] Azure connectivity check: {Status}", azureStatus.Status);
        }
        catch (Exception ex)
        {
            azureStatus.Status = "Error";
            azureStatus.Message = "Cannot reach Azure services";
            allOk = false;
            _logger.LogError(ex, "[Diag] Azure connectivity check failed");
        }
        results.Add(azureStatus);

        // 3. Health check (self)
        var healthStatus = new DiagCheckResult
        {
            Name = "Health Check",
            Status = "OK",
            Message = "Application is running and responding."
        };
        results.Add(healthStatus);
        _logger.LogInformation("[Diag] Health check OK");

        // 4. Backend API check (self)
        var apiStatus = new DiagCheckResult
        {
            Name = "Backend API",
            Status = "OK",
            Message = "API is running and accessible."
        };
        results.Add(apiStatus);
        _logger.LogInformation("[Diag] Backend API check OK");

        // 5. Azure Table Storage check (Azurite if local)
        var tableStatus = new DiagCheckResult { Name = "Azure Table Storage (Azurite)" };
        try
        {
            await _tableClient.CreateIfNotExistsAsync();
            var enumerator = _tableClient.QueryAsync<TableEntity>().GetAsyncEnumerator();
            if (await enumerator.MoveNextAsync())
            {
                // At least one entity exists
            }
            await enumerator.DisposeAsync();
            tableStatus.Status = "OK";
            tableStatus.Message = "Table Storage is accessible (Azurite local emulator).";
            _logger.LogInformation("[Diag] Table Storage check OK");
        }
        catch (Exception ex)
        {
            tableStatus.Status = "Error";
            tableStatus.Message = ex.Message;
            allOk = false;
            _logger.LogError(ex, "[Diag] Table Storage check failed");
        }
        results.Add(tableStatus);

        // 6. Azure OpenAI check
        var openAIStatus = new DiagCheckResult { Name = "Azure OpenAI" };
        try
        {
            var endpoint = _configuration["AzureOpenAI:Endpoint"];            if (!string.IsNullOrEmpty(endpoint))
            {
                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                var openAIResponse = await httpClient.GetAsync(endpoint);
                openAIStatus.Status = "OK";
                openAIStatus.Message = "Azure OpenAI endpoint is reachable.";
                _logger.LogInformation("[Diag] Azure OpenAI check OK");
            }
            else
            {
                openAIStatus.Status = "Warning";
                openAIStatus.Message = "Azure OpenAI endpoint not configured.";
                _logger.LogWarning("[Diag] Azure OpenAI endpoint not configured");
            }
        }
        catch (Exception ex)
        {
            openAIStatus.Status = "Error";
            openAIStatus.Message = "Cannot reach Azure OpenAI service";
            _logger.LogError(ex, "[Diag] Azure OpenAI check failed");
        }
        results.Add(openAIStatus);        _logger.LogInformation("[Diag] Diagnostic checks completed. Overall status: {Status}", allOk ? "OK" : "Issues detected");

        var response = new DiagHealthResponse
        {
            Status = allOk ? "OK" : "Issues detected",
            Timestamp = DateTime.UtcNow,
            Checks = results
        };

        return Ok(response);
    }
}

public class DiagCheckResult
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class DiagHealthResponse
{
    public string Status { get; set; } = string.Empty;
    public List<DiagCheckResult> Checks { get; set; } = new();
    public DateTime Timestamp { get; set; }
}