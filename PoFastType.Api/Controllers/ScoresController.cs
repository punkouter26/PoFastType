using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoFastType.Shared.Models;
using PoFastType.Api.Services;

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
        try
        {
            // Get user identity (works for both authenticated and anonymous users)
            var userIdentity = _identityService.GetCurrentUserIdentity(HttpContext);

            _logger.LogInformation("Submitting score for user: {UserId} ({IdentityType})", 
                userIdentity.UserId, userIdentity.IdentityType);            var result = await _gameService.SubmitGameResultAsync(gameResult, userIdentity.UserId, userIdentity.Username);

            _logger.LogInformation("Score submitted successfully for user {UserId}", userIdentity.UserId);
            return Ok(new { 
                message = "Score submitted successfully", 
                gameId = result.RowKey,
                timestamp = result.Timestamp 
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid game result submitted");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
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
        // Validate top parameter
        if (top <= 0 || top > 100)
        {
            return BadRequest($"Top count must be between 1 and 100. Provided: {top}");
        }
        
        try
        {
            var leaderboard = await _gameService.GetLeaderboardAsync(top);
            
            _logger.LogInformation("Retrieved leaderboard with {Count} entries", leaderboard.Count());
            return Ok(leaderboard);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid leaderboard request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leaderboard");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Failed to retrieve leaderboard" });
        }
    }

    /// <summary>
    /// Retrieves user statistics for the authenticated user
    /// </summary>
    [HttpGet("me/stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserStats()
    {
        try
        {
            var userIdentity = _identityService.GetCurrentUserIdentity(HttpContext);

            _logger.LogInformation("Getting stats for user: {UserId} ({IdentityType})", 
                userIdentity.UserId, userIdentity.IdentityType);

            var userStats = await _gameService.GetUserStatsAsync(userIdentity.UserId);

            _logger.LogInformation("Retrieved {Count} game results for user {UserId}", 
                userStats.Count(), userIdentity.UserId);

            return Ok(userStats);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid user stats request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user stats");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Failed to retrieve user stats" });
        }
    }
}