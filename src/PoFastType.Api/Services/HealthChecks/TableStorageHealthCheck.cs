using Azure.Data.Tables;

namespace PoFastType.Api.Services.HealthChecks;

/// <summary>
/// Health check strategy for verifying Azure Table Storage connectivity.
/// </summary>
public class TableStorageHealthCheck : IHealthCheckStrategy
{
    private readonly TableClient _tableClient;
    private readonly ILogger<TableStorageHealthCheck> _logger;

    public string CheckName => "Azure Table Storage (Azurite)";

    public TableStorageHealthCheck(IConfiguration configuration, ILogger<TableStorageHealthCheck> logger)
    {
        _logger = logger;
        var connectionString = configuration["AzureTableStorage:ConnectionString"];
        var tableName = configuration["AzureTableStorage:TableName"];
        _tableClient = new TableClient(connectionString, tableName);
    }

    public async Task<DiagCheckResult> ExecuteAsync()
    {
        var result = new DiagCheckResult { Name = CheckName };

        try
        {
            await _tableClient.CreateIfNotExistsAsync();
            var enumerator = _tableClient.QueryAsync<TableEntity>().GetAsyncEnumerator();
            if (await enumerator.MoveNextAsync())
            {
                // At least one entity exists
            }
            await enumerator.DisposeAsync();

            result.Status = "OK";
            result.Message = "Table Storage is accessible (Azurite local emulator).";
            _logger.LogInformation("[Diag] {CheckName} check OK", CheckName);
        }
        catch (Exception ex)
        {
            result.Status = "Error";
            result.Message = ex.Message;
            _logger.LogError(ex, "[Diag] {CheckName} check failed", CheckName);
        }

        return result;
    }
}
