using Microsoft.Extensions.Diagnostics.HealthChecks;
using Azure.Data.Tables;

namespace PoFastType.Api.HealthChecks;

/// <summary>
/// Health check for Azure Table Storage connectivity
/// </summary>
public class AzureTableStorageHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureTableStorageHealthCheck> _logger;

    public AzureTableStorageHealthCheck(
        IConfiguration configuration,
        ILogger<AzureTableStorageHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _configuration["AzureTableStorage:ConnectionString"];
            if (string.IsNullOrEmpty(connectionString))
            {
                return HealthCheckResult.Unhealthy(
                    "Azure Table Storage connection string not configured");
            }

            var tableName = _configuration["AzureTableStorage:TableName"] ?? "PoFastTypeGameResults";
            var tableClient = new TableClient(connectionString, tableName);

            // Try to query the table (will create if not exists with Azurite)
            await tableClient.CreateIfNotExistsAsync(cancellationToken);
            
            // Verify we can query
            var query = tableClient.QueryAsync<TableEntity>(
                maxPerPage: 1,
                cancellationToken: cancellationToken);
            
            await using var enumerator = query.GetAsyncEnumerator(cancellationToken);
            await enumerator.MoveNextAsync(); // Just check if we can access it

            return HealthCheckResult.Healthy(
                $"Azure Table Storage is accessible (Table: {tableName})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Table Storage health check failed");
            return HealthCheckResult.Unhealthy(
                "Azure Table Storage is not accessible",
                ex);
        }
    }
}
