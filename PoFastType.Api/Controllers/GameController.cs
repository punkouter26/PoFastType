using Microsoft.AspNetCore.Mvc;
using PoFastType.Api.Services;

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
        try
        {
            _logger.LogInformation("Generating new text for typing test");
            var text = await _textGenerationService.GenerateTextAsync();
            
            return Ok(new { text, timestamp = DateTime.UtcNow });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when generating text");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Text generation is currently unavailable" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating text for typing test");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "An unexpected error occurred" });
        }
    }
}