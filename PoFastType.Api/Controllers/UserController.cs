using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoFastType.Shared.Models;
using PoFastType.Api.Services;
using Azure.Data.Tables;

namespace PoFastType.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly TableClient _tableClient;
    private readonly ILogger<UserController> _logger;
    private readonly IUserIdentityService _identityService;

    public UserController(
        IConfiguration configuration,
        ILogger<UserController> logger,
        IUserIdentityService identityService)
    {
        var connectionString = configuration["AzureTableStorage:ConnectionString"];
        var tableName = configuration["AzureTableStorage:TableName"];

        if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(tableName))
        {
            throw new InvalidOperationException("Azure Table Storage configuration is missing");
        }

        _tableClient = new TableClient(connectionString, tableName);
        _logger = logger;
        _identityService = identityService;
    }
    [HttpGet("profile")]
    public async Task<IActionResult> GetUserProfile()
    {
        try
        {
            // Always use ANON user
            var userIdentity = _identityService.GetCurrentUserIdentity(HttpContext);

            _logger.LogInformation("Getting profile for ANON user");

            // Query all scores for ANON user to calculate statistics
            await _tableClient.CreateIfNotExistsAsync();
            var query = _tableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{userIdentity.UserId}'");
            var userScores = new List<UserGameResult>();

            await foreach (var entity in query)
            {
                if (entity.TryGetValue("NetWPM", out var netWpm) &&
                    entity.TryGetValue("Accuracy", out var accuracy) &&
                    entity.TryGetValue("GrossWPM", out var grossWpm) &&
                    entity.TryGetValue("ProblemKeysJson", out var problemKeysJson) &&
                    entity.TryGetValue("Timestamp", out var timestamp))
                {
                    userScores.Add(new UserGameResult
                    {
                        NetWPM = Convert.ToDouble(netWpm),
                        Accuracy = Convert.ToDouble(accuracy),
                        GrossWPM = Convert.ToDouble(grossWpm),
                        ProblemKeysJson = problemKeysJson.ToString() ?? "{}",
                        Timestamp = timestamp as DateTime? ?? DateTime.UtcNow
                    });
                }
            }            // Calculate profile statistics
            var profile = new UserProfile
            {
                UserId = userIdentity.UserId,
                Username = userIdentity.Username,
                Email = userIdentity.Email,
                CreatedAt = userScores.Any() ? userScores.Min(s => s.Timestamp) : DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                TotalTestsCompleted = userScores.Count,
                BestNetWPM = userScores.Any() ? userScores.Max(s => s.NetWPM) : 0,
                AverageNetWPM = userScores.Any() ? userScores.Average(s => s.NetWPM) : 0,
                AverageAccuracy = userScores.Any() ? userScores.Average(s => s.Accuracy) : 0,
                TotalTypingTime = TimeSpan.FromMinutes(userScores.Count * 1.0) // Assuming 1 minute per test
            };

            _logger.LogInformation("Retrieved user profile for {UserId}: {TestsCompleted} tests completed",
                userIdentity.UserId, profile.TotalTestsCompleted);

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return StatusCode(500, new { error = "Failed to retrieve user profile" });
        }
    }
    [HttpGet("identity")]
    public IActionResult GetUserIdentity()
    {
        try
        {
            // Always return ANON user identity
            var userIdentity = _identityService.GetCurrentUserIdentity(HttpContext);

            _logger.LogInformation("Getting identity for ANON user");

            return Ok(userIdentity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user identity");
            return StatusCode(500, new { error = "Failed to retrieve user identity" });
        }
    }

    public class UserGameResult
    {
        public double NetWPM { get; set; }
        public double Accuracy { get; set; }
        public double GrossWPM { get; set; }
        public string ProblemKeysJson { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}