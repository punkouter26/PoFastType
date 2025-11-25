using Microsoft.AspNetCore.Mvc;
using PoFastType.Api.Services;
using PoFastType.Api.Repositories;
using PoFastType.Api.Extensions;
using PoFastType.Shared.Models;
using Serilog;

namespace PoFastType.Api.Controllers;

/// <summary>
/// API Controller for biometric keystroke tracking and analytics
/// Follows Single Responsibility Principle - handles HTTP concerns for biometrics endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BiometricsController : ControllerBase
{
    private readonly IBiometricsService _biometricsService;
    private readonly IKeystrokeRepository _keystrokeRepository;

    public BiometricsController(
        IBiometricsService biometricsService,
        IKeystrokeRepository keystrokeRepository)
    {
        ArgumentNullException.ThrowIfNull(biometricsService);
        ArgumentNullException.ThrowIfNull(keystrokeRepository);
        
        _biometricsService = biometricsService;
        _keystrokeRepository = keystrokeRepository;
    }

    /// <summary>
    /// Submit a single keystroke event
    /// </summary>
    /// <param name="keystroke">Keystroke data to record</param>
    /// <returns>Recorded keystroke with confirmation</returns>
    [HttpPost("keystroke")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SubmitKeystroke([FromBody] KeystrokeData keystroke)
    {
        var ctx = HttpContext.GetRequestContext();

        if (keystroke == null)
        {
            Log.Warning("Keystroke submission failed - null data (IP: {UserIP}, RequestId: {RequestId})", 
                ctx.UserIP, ctx.RequestId);
            return BadRequest(new { error = "Keystroke data cannot be null" });
        }

        if (string.IsNullOrWhiteSpace(keystroke.PartitionKey))
        {
            Log.Warning("Keystroke submission failed - missing user ID (IP: {UserIP}, RequestId: {RequestId})", 
                ctx.UserIP, ctx.RequestId);
            return BadRequest(new { error = "User ID is required" });
        }

        try
        {
            Log.Information("Biometrics: Keystroke received for user {UserId}, game {GameId}, seq {Seq} (IP: {UserIP}, RequestId: {RequestId})",
                keystroke.PartitionKey, keystroke.GameId, keystroke.SequenceNumber, ctx.UserIP, ctx.RequestId);

            var result = await _keystrokeRepository.AddKeystrokeAsync(keystroke);

            return Ok(new
            {
                success = true,
                keystroke = result,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to submit keystroke for user {UserId} (IP: {UserIP}, RequestId: {RequestId})",
                keystroke.PartitionKey, ctx.UserIP, ctx.RequestId);
            return StatusCode(500, new { error = "Failed to submit keystroke" });
        }
    }

    /// <summary>
    /// Submit multiple keystrokes in a batch (more efficient)
    /// </summary>
    /// <param name="keystrokes">Collection of keystroke data</param>
    /// <returns>Batch submission confirmation</returns>
    [HttpPost("keystrokes/batch")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SubmitKeystrokesBatch([FromBody] List<KeystrokeData> keystrokes)
    {
        var ctx = HttpContext.GetRequestContext();

        if (keystrokes == null || !keystrokes.Any())
        {
            Log.Warning("Batch keystroke submission failed - empty data (IP: {UserIP}, RequestId: {RequestId})", 
                ctx.UserIP, ctx.RequestId);
            return BadRequest(new { error = "Keystroke data cannot be empty" });
        }

        var userId = keystrokes.First().PartitionKey;

        try
        {
            Log.Information("Biometrics: Batch of {Count} keystrokes received for user {UserId} (IP: {UserIP}, RequestId: {RequestId})",
                keystrokes.Count, userId, ctx.UserIP, ctx.RequestId);

            await _keystrokeRepository.AddKeystrokesBatchAsync(keystrokes);

            return Ok(new
            {
                success = true,
                count = keystrokes.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to submit keystroke batch for user {UserId} (IP: {UserIP}, RequestId: {RequestId})",
                userId, ctx.UserIP, ctx.RequestId);
            return StatusCode(500, new { error = "Failed to submit keystroke batch" });
        }
    }

    /// <summary>
    /// Get comprehensive biometric statistics for a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>Complete biometric statistics and heatmap data</returns>
    [HttpGet("user/{userId}/stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserBiometrics(string userId)
    {
        var ctx = HttpContext.GetRequestContext();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest(new { error = "User ID is required" });
        }

        try
        {
            Log.Information("Biometrics: Stats requested for user {UserId} (IP: {UserIP}, RequestId: {RequestId})",
                userId, ctx.UserIP, ctx.RequestId);

            var stats = await _biometricsService.CalculateUserBiometricsAsync(userId);

            if (stats.TotalKeystrokes == 0)
            {
                Log.Information("No biometric data found for user {UserId}", userId);
                return NotFound(new { error = "No keystroke data found for this user" });
            }

            return Ok(stats);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get biometrics for user {UserId} (IP: {UserIP}, RequestId: {RequestId})",
                userId, ctx.UserIP, ctx.RequestId);
            return StatusCode(500, new { error = "Failed to retrieve biometric statistics" });
        }
    }

    /// <summary>
    /// Get keyboard heatmap data for visualization
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>List of key metrics with accuracy and speed data</returns>
    [HttpGet("user/{userId}/heatmap")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetKeyboardHeatmap(string userId)
    {
        var ctx = HttpContext.GetRequestContext();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest(new { error = "User ID is required" });
        }

        try
        {
            Log.Information("Biometrics: Heatmap requested for user {UserId} (IP: {UserIP}, RequestId: {RequestId})",
                userId, ctx.UserIP, ctx.RequestId);

            var heatmap = await _biometricsService.GetKeyboardHeatmapAsync(userId);

            return Ok(new
            {
                userId,
                heatmap,
                totalKeys = heatmap.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get heatmap for user {UserId} (IP: {UserIP}, RequestId: {RequestId})",
                userId, ctx.UserIP, ctx.RequestId);
            return StatusCode(500, new { error = "Failed to retrieve keyboard heatmap" });
        }
    }

    /// <summary>
    /// Get problem keys (low accuracy or high error rate)
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="count">Number of problem keys to return (default: 10)</param>
    /// <returns>List of keys with lowest accuracy</returns>
    [HttpGet("user/{userId}/problem-keys")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProblemKeys(string userId, [FromQuery] int count = 10)
    {
        var ctx = HttpContext.GetRequestContext();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest(new { error = "User ID is required" });
        }

        if (count <= 0 || count > 50)
        {
            return BadRequest(new { error = "Count must be between 1 and 50" });
        }

        try
        {
            var problemKeys = await _biometricsService.GetProblemKeysAsync(userId, count);

            return Ok(new
            {
                userId,
                problemKeys,
                count = problemKeys.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get problem keys for user {UserId} (IP: {UserIP}, RequestId: {RequestId})",
                userId, ctx.UserIP, ctx.RequestId);
            return StatusCode(500, new { error = "Failed to retrieve problem keys" });
        }
    }

    /// <summary>
    /// Get error patterns (commonly confused keys)
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>List of detected error patterns</returns>
    [HttpGet("user/{userId}/error-patterns")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetErrorPatterns(string userId)
    {
        var ctx = HttpContext.GetRequestContext();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest(new { error = "User ID is required" });
        }

        try
        {
            Log.Information("Biometrics: Error patterns requested for user {UserId} (IP: {UserIP}, RequestId: {RequestId})",
                userId, ctx.UserIP, ctx.RequestId);

            var patterns = await _biometricsService.DetectErrorPatternsAsync(userId);

            return Ok(new
            {
                userId,
                patterns,
                count = patterns.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get error patterns for user {UserId} (IP: {UserIP}, RequestId: {RequestId})",
                userId, ctx.UserIP, ctx.RequestId);
            return StatusCode(500, new { error = "Failed to retrieve error patterns" });
        }
    }

    /// <summary>
    /// Get biometrics for a specific game session
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="gameId">Game session identifier</param>
    /// <returns>Biometric statistics for the specific game</returns>
    [HttpGet("user/{userId}/game/{gameId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetGameBiometrics(string userId, string gameId)
    {
        var ctx = HttpContext.GetRequestContext();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest(new { error = "User ID is required" });
        }

        if (string.IsNullOrWhiteSpace(gameId))
        {
            return BadRequest(new { error = "Game ID is required" });
        }

        try
        {
            Log.Information("Biometrics: Game stats requested for user {UserId}, game {GameId} (IP: {UserIP}, RequestId: {RequestId})",
                userId, gameId, ctx.UserIP, ctx.RequestId);

            var stats = await _biometricsService.CalculateGameBiometricsAsync(userId, gameId);

            if (stats.TotalKeystrokes == 0)
            {
                return NotFound(new { error = "No keystroke data found for this game" });
            }

            return Ok(stats);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get game biometrics for user {UserId}, game {GameId} (IP: {UserIP}, RequestId: {RequestId})",
                userId, gameId, ctx.UserIP, ctx.RequestId);
            return StatusCode(500, new { error = "Failed to retrieve game biometric statistics" });
        }
    }

    /// <summary>
    /// Delete all keystroke data for a user (GDPR compliance)
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("user/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteUserData(string userId)
    {
        var ctx = HttpContext.GetRequestContext();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest(new { error = "User ID is required" });
        }

        try
        {
            Log.Warning("Biometrics: Data deletion requested for user {UserId} (IP: {UserIP}, RequestId: {RequestId})",
                userId, ctx.UserIP, ctx.RequestId);

            await _keystrokeRepository.DeleteUserKeystrokesAsync(userId);

            Log.Information("Biometrics: All data deleted for user {UserId} (IP: {UserIP}, RequestId: {RequestId})",
                userId, ctx.UserIP, ctx.RequestId);

            return Ok(new
            {
                success = true,
                message = "All keystroke data has been deleted",
                userId,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete data for user {UserId} (IP: {UserIP}, RequestId: {RequestId})",
                userId, ctx.UserIP, ctx.RequestId);
            return StatusCode(500, new { error = "Failed to delete user data" });
        }
    }
}
