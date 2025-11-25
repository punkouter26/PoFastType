using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoFastType.Shared.Models;
using PoFastType.Api.Services;
using PoFastType.Api.Extensions;
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

    public ScoresController(IGameService gameService)
    {
        ArgumentNullException.ThrowIfNull(gameService);
        _gameService = gameService;
    }    /// <summary>
         /// Submits a game result for any user (authenticated or anonymous)
         /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SubmitScore([FromBody] GameResult gameResult)
    {
        var ctx = HttpContext.GetRequestContext();

        try
        {
            // Always use ANON user for high scores
            Log.Information("User action: Score submission - NetWPM: {NetWPM}, Accuracy: {Accuracy}% (IP: {UserIP}, RequestId: {RequestId})",
                gameResult.NetWPM, gameResult.Accuracy, ctx.UserIP, ctx.RequestId);

            var result = await _gameService.SubmitGameResultAsync(gameResult, "ANON", "ANON");

            Log.Information("Game state change: Score saved with ID {GameId} - NetWPM: {NetWPM}, Accuracy: {Accuracy}% (IP: {UserIP}, RequestId: {RequestId})",
                result.RowKey, gameResult.NetWPM, gameResult.Accuracy, ctx.UserIP, ctx.RequestId);

            return Ok(new
            {
                message = "Score submitted successfully",
                gameId = result.RowKey,
                timestamp = result.Timestamp
            });
        }
        catch (ArgumentException ex)
        {
            Log.Warning(ex, "Invalid score submission (IP: {UserIP}, RequestId: {RequestId})", ctx.UserIP, ctx.RequestId);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Score submission failed (IP: {UserIP}, RequestId: {RequestId})", ctx.UserIP, ctx.RequestId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to submit score" });
        }
    }    /// <summary>
         /// Retrieves the leaderboard with top performers
         /// </summary>
    [HttpGet("leaderboard")]
    [HttpGet("")] // RESTful alias: GET /api/scores returns leaderboard
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int top = 10)
    {
        var ctx = HttpContext.GetRequestContext();

        // Validate top parameter
        if (top <= 0 || top > 100)
        {
            Log.Warning("Invalid leaderboard request - invalid top parameter: {Top} (IP: {UserIP}, RequestId: {RequestId})", 
                top, ctx.UserIP, ctx.RequestId);
            return BadRequest($"Top count must be between 1 and 100. Provided: {top}");
        }

        try
        {
            Log.Information("User action: Leaderboard requested (top {Top}) (IP: {UserIP}, RequestId: {RequestId})", 
                top, ctx.UserIP, ctx.RequestId);

            var leaderboard = await _gameService.GetLeaderboardAsync(top);

            Log.Information("Leaderboard data retrieved - {Count} entries returned (IP: {UserIP}, RequestId: {RequestId})",
                leaderboard.Count(), ctx.UserIP, ctx.RequestId);

            return Ok(leaderboard);
        }
        catch (ArgumentException ex)
        {
            Log.Warning(ex, "Invalid leaderboard request (IP: {UserIP}, RequestId: {RequestId})", ctx.UserIP, ctx.RequestId);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Leaderboard retrieval failed (IP: {UserIP}, RequestId: {RequestId})", ctx.UserIP, ctx.RequestId);
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
        var ctx = HttpContext.GetRequestContext();

        try
        {
            Log.Information("User action: User stats requested (IP: {UserIP}, RequestId: {RequestId})", ctx.UserIP, ctx.RequestId);

            // Since all users are ANON, all stats are shared
            var userStats = await _gameService.GetUserStatsAsync("ANON");

            Log.Information("User stats retrieved - {Count} game results (IP: {UserIP}, RequestId: {RequestId})",
                userStats.Count(), ctx.UserIP, ctx.RequestId);

            return Ok(userStats);
        }
        catch (ArgumentException ex)
        {
            Log.Warning(ex, "Invalid user stats request (IP: {UserIP}, RequestId: {RequestId})", ctx.UserIP, ctx.RequestId);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "User stats retrieval failed (IP: {UserIP}, RequestId: {RequestId})", ctx.UserIP, ctx.RequestId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve user stats" });
        }
    }
}