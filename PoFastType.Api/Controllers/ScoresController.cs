using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoFastType.Shared.Models;
using PoFastType.Api.Services;
using Serilog;

namespace PoFastType.Api.Controllers;

/// <summary>
/// Single Responsibility Principle (SOLID) - Handles only HTTP concerns for score-related endpoints
/// Dependency Inversion Principle (SOLID) - Depends on IGameService abstraction
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ScoresController : ControllerBase
{
    private readonly IGameService _gameService;
    private readonly IUserIdentityService _identityService;
    private readonly ILogger<ScoresController> _logger;

    public ScoresController(
        IGameService gameService,
        IUserIdentityService identityService,
        ILogger<ScoresController> logger)
    {
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }    /// <summary>
         /// Submits a game result for any user (authenticated or anonymous)
         /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SubmitScore([FromBody] GameResult gameResult)
    {
        var requestId = HttpContext.TraceIdentifier;
        var userIP = HttpContext.Connection.RemoteIpAddress?.ToString();

        try
        {
            // Always use ANON user for high scores
            var userIdentity = _identityService.GetCurrentUserIdentity(HttpContext);

            Log.Information("User action: Score submission by {UserIP} - NetWPM: {NetWPM}, Accuracy: {Accuracy}% (RequestId: {RequestId})",
                userIP, gameResult.NetWPM, gameResult.Accuracy, requestId);

            _logger.LogInformation("Submitting score for ANON user");

            var result = await _gameService.SubmitGameResultAsync(gameResult, userIdentity.UserId, "ANON");

            Log.Information("Game state change: Score saved with ID {GameId} for user {UserIP} - NetWPM: {NetWPM}, Accuracy: {Accuracy}% (RequestId: {RequestId})",
                result.RowKey, userIP, gameResult.NetWPM, gameResult.Accuracy, requestId);

            _logger.LogInformation("Score submitted successfully for ANON user");
            return Ok(new
            {
                message = "Score submitted successfully",
                gameId = result.RowKey,
                timestamp = result.Timestamp
            });
        }
        catch (ArgumentException ex)
        {
            Log.Warning(ex, "Invalid score submission by user {UserIP} (RequestId: {RequestId})", userIP, requestId);
            _logger.LogWarning(ex, "Invalid game result submitted");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Score submission failed for user {UserIP} (RequestId: {RequestId})", userIP, requestId);
            _logger.LogError(ex, "Error submitting score");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to submit score" });
        }
    }    /// <summary>
         /// Retrieves the leaderboard with top performers
         /// </summary>
    [HttpGet("leaderboard")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int top = 10)
    {
        var requestId = HttpContext.TraceIdentifier;
        var userIP = HttpContext.Connection.RemoteIpAddress?.ToString();

        // Validate top parameter
        if (top <= 0 || top > 100)
        {
            Log.Warning("Invalid leaderboard request by user {UserIP} - invalid top parameter: {Top} (RequestId: {RequestId})", userIP, top, requestId);
            return BadRequest($"Top count must be between 1 and 100. Provided: {top}");
        }

        try
        {
            Log.Information("User action: Leaderboard requested by {UserIP} (top {Top}) (RequestId: {RequestId})", userIP, top, requestId);

            var leaderboard = await _gameService.GetLeaderboardAsync(top);

            Log.Information("Leaderboard data retrieved for user {UserIP} - {Count} entries returned (RequestId: {RequestId})",
                userIP, leaderboard.Count(), requestId);

            _logger.LogInformation("Retrieved leaderboard with {Count} entries", leaderboard.Count());
            return Ok(leaderboard);
        }
        catch (ArgumentException ex)
        {
            Log.Warning(ex, "Invalid leaderboard request by user {UserIP} (RequestId: {RequestId})", userIP, requestId);
            _logger.LogWarning(ex, "Invalid leaderboard request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Leaderboard retrieval failed for user {UserIP} (RequestId: {RequestId})", userIP, requestId);
            _logger.LogError(ex, "Error retrieving leaderboard");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve leaderboard" });
        }
    }    /// <summary>
         /// Retrieves user statistics - since all users are ANON, returns shared stats
         /// </summary>
    [HttpGet("me/stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserStats()
    {
        var requestId = HttpContext.TraceIdentifier;
        var userIP = HttpContext.Connection.RemoteIpAddress?.ToString();

        try
        {
            var userIdentity = _identityService.GetCurrentUserIdentity(HttpContext);

            Log.Information("User action: User stats requested by {UserIP} (RequestId: {RequestId})", userIP, requestId);

            _logger.LogInformation("Getting shared stats for ANON user");

            // Since all users are ANON, all stats are shared
            var userStats = await _gameService.GetUserStatsAsync(userIdentity.UserId);

            Log.Information("User stats retrieved for user {UserIP} - {Count} game results (RequestId: {RequestId})",
                userIP, userStats.Count(), requestId);

            _logger.LogInformation("Retrieved {Count} game results for ANON user",
                userStats.Count());

            return Ok(userStats);
        }
        catch (ArgumentException ex)
        {
            Log.Warning(ex, "Invalid user stats request by user {UserIP} (RequestId: {RequestId})", userIP, requestId);
            _logger.LogWarning(ex, "Invalid user stats request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "User stats retrieval failed for user {UserIP} (RequestId: {RequestId})", userIP, requestId);
            _logger.LogError(ex, "Error retrieving user stats");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve user stats" });
        }
    }
}