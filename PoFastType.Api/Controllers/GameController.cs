using Microsoft.AspNetCore.Mvc;
using PoFastType.Api.Services;
using Serilog;

namespace PoFastType.Api.Controllers;

/// <summary>
/// Single Responsibility Principle (SOLID) - Handles only HTTP concerns for game-related endpoints
/// Dependency Inversion Principle (SOLID) - Depends on ITextGenerationService abstraction
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly ITextGenerationService _textGenerationService;
    private readonly ILogger<GameController> _logger;

    public GameController(ITextGenerationService textGenerationService, ILogger<GameController> logger)
    {
        _textGenerationService = textGenerationService ?? throw new ArgumentNullException(nameof(textGenerationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates text for typing practice
    /// </summary>
    /// <returns>Generated text for typing test</returns>
    [HttpGet("text")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetText()
    {
        var requestId = HttpContext.TraceIdentifier;
        var userIP = HttpContext.Connection.RemoteIpAddress?.ToString();
        
        try
        {
            Log.Information("User action: Text generation requested by {UserIP} (RequestId: {RequestId})", userIP, requestId);
            _logger.LogInformation("Generating new text for typing test");
            
            var text = await _textGenerationService.GenerateTextAsync();

            Log.Information("Game state change: New text generated with length {TextLength} for user {UserIP} (RequestId: {RequestId})", 
                text.Length, userIP, requestId);

            return Ok(new { text, timestamp = DateTime.UtcNow });
        }
        catch (InvalidOperationException ex)
        {
            Log.Warning(ex, "Text generation failed for user {UserIP} - Invalid operation (RequestId: {RequestId})", userIP, requestId);
            _logger.LogWarning(ex, "Invalid operation when generating text");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Text generation is currently unavailable" });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Text generation failed for user {UserIP} - Unexpected error (RequestId: {RequestId})", userIP, requestId);
            _logger.LogError(ex, "Unexpected error generating text for typing test");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An unexpected error occurred" });
        }
    }
}