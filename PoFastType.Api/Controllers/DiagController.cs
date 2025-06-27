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

        // Table Storage check
        var tableStatus = new DiagCheckResult { Name = "Azure Table Storage" };
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
            tableStatus.Message = "Table Storage is accessible.";
            _logger.LogInformation("[Diag] Table Storage check OK");
        }        catch (Exception ex)
        {
            tableStatus.Status = "Error";
            tableStatus.Message = ex.Message;
            allOk = false;
            _logger.LogError(ex, "[Diag] Table Storage check failed");
        }
        results.Add(tableStatus);

        // Backend API check (self)
        var apiStatus = new DiagCheckResult
        {
            Name = "Backend API",
            Status = "OK",
            Message = "API is running."
        };
        results.Add(apiStatus);
        _logger.LogInformation("[Diag] Backend API check OK");        // Internet connectivity check
        var internetStatus = new DiagCheckResult { Name = "Internet Connectivity" };
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            var response = await httpClient.GetAsync("https://www.msftconnecttest.com/connecttest.txt");
            if (response.IsSuccessStatusCode)
            {
                internetStatus.Status = "OK";
                internetStatus.Message = "Online";
                _logger.LogInformation("[Diag] Internet connectivity check OK");
            }
            else
            {
                internetStatus.Status = "Error";
                internetStatus.Message = $"HTTP {response.StatusCode}";
                allOk = false;
                _logger.LogWarning("[Diag] Internet connectivity check failed with status {StatusCode}", response.StatusCode);
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

        // Network ping check
        var pingStatus = new DiagCheckResult { Name = "Network Ping" };
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync("8.8.8.8", 5000);
            if (reply.Status == IPStatus.Success)
            {
                pingStatus.Status = "OK";
                pingStatus.Message = $"Ping successful: {reply.RoundtripTime}ms";
                _logger.LogInformation("[Diag] Network ping check OK: {RoundtripTime}ms", reply.RoundtripTime);
            }
            else
            {
                pingStatus.Status = "Error";
                pingStatus.Message = $"Ping failed: {reply.Status}";
                allOk = false;
                _logger.LogWarning("[Diag] Network ping check failed: {Status}", reply.Status);
            }
        }
        catch (Exception ex)
        {
            pingStatus.Status = "Error";
            pingStatus.Message = "Ping failed with exception.";
            allOk = false;
            _logger.LogError(ex, "[Diag] Network ping check failed");
        }
        results.Add(pingStatus);

        // Azure service connectivity check
        var azureStatus = new DiagCheckResult { Name = "Azure Service Connectivity" };
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            var response = await httpClient.GetAsync("https://management.azure.com/");
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) // Expected for unauthenticated call
            {
                azureStatus.Status = "OK";
                azureStatus.Message = "Azure services reachable";
                _logger.LogInformation("[Diag] Azure service connectivity check OK");
            }
            else
            {
                azureStatus.Status = "Warning";
                azureStatus.Message = $"Unexpected response: {response.StatusCode}";
                _logger.LogWarning("[Diag] Azure service connectivity check unexpected response: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            azureStatus.Status = "Error";
            azureStatus.Message = "Cannot reach Azure services.";
            allOk = false;
            _logger.LogError(ex, "[Diag] Azure service connectivity check failed");
        }
        results.Add(azureStatus);

        // Note: Removed self-referential health check to prevent infinite recursion

        // Backend API functionality check (game/text endpoint)
        var backendApiStatus = new DiagCheckResult { Name = "Backend API Functionality" };
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            var baseUrl = _configuration["BaseUrl"] ?? "http://localhost:5000";
            var response = await httpClient.GetAsync($"{baseUrl}/api/game/text");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    backendApiStatus.Status = "OK";
                    backendApiStatus.Message = "API endpoints functioning";
                    _logger.LogInformation("[Diag] Backend API functionality check OK");
                }
                else
                {
                    backendApiStatus.Status = "Warning";
                    backendApiStatus.Message = "API returned empty response";
                    _logger.LogWarning("[Diag] Backend API functionality check: empty response");
                }
            }
            else
            {
                backendApiStatus.Status = "Error";
                backendApiStatus.Message = $"API error: {response.StatusCode}";
                allOk = false;
                _logger.LogError("[Diag] Backend API functionality check failed: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            backendApiStatus.Status = "Error";
            backendApiStatus.Message = "API functionality test failed.";
            allOk = false;
            _logger.LogError(ex, "[Diag] Backend API functionality check failed");
        }
        results.Add(backendApiStatus);

        // Azure Table Storage/Azurite connectivity check
        var azuriteStatus = new DiagCheckResult { Name = "Azure Table Storage/Azurite" };
        try
        {
            var connectionString = _configuration["AzureTableStorage:ConnectionString"];
            if (string.IsNullOrEmpty(connectionString) || connectionString.Contains("YOUR_AZURE_TABLE_STORAGE"))
            {
                azuriteStatus.Status = "Warning";
                azuriteStatus.Message = "Not configured (using placeholder values)";
                _logger.LogWarning("[Diag] Azure Table Storage not configured");
            }
            else if (connectionString.Contains("UseDevelopmentStorage=true"))
            {
                // Test Azurite local storage emulator
                var tempTableClient = new TableClient(connectionString, "DiagTest");
                await tempTableClient.CreateIfNotExistsAsync();
                await tempTableClient.DeleteAsync(); // Clean up test table
                azuriteStatus.Status = "OK";
                azuriteStatus.Message = "Azurite development storage accessible";
                _logger.LogInformation("[Diag] Azurite storage check OK");
            }
            else
            {
                // Test real Azure Table Storage
                var tempTableClient = new TableClient(connectionString, "DiagTest");
                await tempTableClient.CreateIfNotExistsAsync();
                await tempTableClient.DeleteAsync(); // Clean up test table
                azuriteStatus.Status = "OK";
                azuriteStatus.Message = "Azure Table Storage accessible";
                _logger.LogInformation("[Diag] Azure Table Storage check OK");
            }
        }
        catch (Exception ex)
        {
            azuriteStatus.Status = "Error";
            azuriteStatus.Message = "Storage connection failed.";
            allOk = false;
            _logger.LogError(ex, "[Diag] Azure Table Storage/Azurite check failed");
        }        results.Add(azuriteStatus);

        // Text generation strategy check
        var textGenStatus = new DiagCheckResult { Name = "Text Generation Service" };
        try
        {
            textGenStatus.Status = "OK";
            textGenStatus.Message = "Using built-in hardcoded text strategy";
            _logger.LogInformation("[Diag] Text generation service check OK - using hardcoded strategy");
        }
        catch (Exception ex)
        {
            textGenStatus.Status = "Error";
            textGenStatus.Message = ex.Message;
            allOk = false;
            _logger.LogError(ex, "[Diag] Text generation service check failed");
        }
        results.Add(textGenStatus);

        // External API dependencies check
        var externalApiStatus = new DiagCheckResult { Name = "External API Dependencies" };
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            
            // Test multiple external services that might be used
            var externalTests = new List<(string name, string url)>
            {
                ("Microsoft Graph", "https://graph.microsoft.com/v1.0/"),
                ("Azure Active Directory", "https://login.microsoftonline.com/common/v2.0/.well-known/openid_configuration")
            };

            var successful = 0;
            var total = externalTests.Count;

            foreach (var (name, url) in externalTests)
            {
                try
                {
                    var response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        successful++;
                        _logger.LogDebug("[Diag] External API {Name} check OK", name);
                    }
                }
                catch
                {
                    _logger.LogDebug("[Diag] External API {Name} check failed", name);
                }
            }

            if (successful == total)
            {
                externalApiStatus.Status = "OK";
                externalApiStatus.Message = $"All external APIs reachable ({successful}/{total})";
            }
            else if (successful > 0)
            {
                externalApiStatus.Status = "Warning";
                externalApiStatus.Message = $"Some external APIs unreachable ({successful}/{total})";
            }
            else
            {
                externalApiStatus.Status = "Error";
                externalApiStatus.Message = "No external APIs reachable";
                allOk = false;
            }
            _logger.LogInformation("[Diag] External API dependencies check: {Successful}/{Total}", successful, total);
        }
        catch (Exception ex)
        {
            externalApiStatus.Status = "Error";
            externalApiStatus.Message = "External API checks failed.";
            allOk = false;
            _logger.LogError(ex, "[Diag] External API dependencies check failed");
        }
        results.Add(externalApiStatus);

        var overall = new
        {
            OverallStatus = allOk ? "OK" : "Error",
            Checks = results,
            Timestamp = DateTime.UtcNow,
            Summary = new
            {
                TotalChecks = results.Count,
                SuccessfulChecks = results.Count(r => r.Status == "OK"),
                WarningChecks = results.Count(r => r.Status == "Warning"),
                ErrorChecks = results.Count(r => r.Status == "Error")
            }
        };

        return Ok(overall);
    }

    public class DiagCheckResult
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // OK or Error
        public string Message { get; set; } = string.Empty;
    }
} 