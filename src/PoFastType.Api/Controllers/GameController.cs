using Microsoft.AspNetCore.Mvc;
using PoFastType.Api.Services;
using PoFastType.Api.Extensions;
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

    public GameController(ITextGenerationService textGenerationService)
    {
        ArgumentNullException.ThrowIfNull(textGenerationService);
        _textGenerationService = textGenerationService;
    }

    /// <summary>
    /// Generates text for typing practice
    /// </summary>
    /// <returns>Generated text for typing test</returns>
    [HttpGet("text")]
    [HttpGet("/api/practice-texts")]  // RESTful collection alias
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetText()
    {
        var ctx = HttpContext.GetRequestContext();

        try
        {
            Log.Information("User action: Text generation requested (IP: {UserIP}, RequestId: {RequestId})", 
                ctx.UserIP, ctx.RequestId);

            var text = await _textGenerationService.GenerateTextAsync();

            Log.Information("Game state change: New text generated with length {TextLength} (IP: {UserIP}, RequestId: {RequestId})",
                text.Length, ctx.UserIP, ctx.RequestId);

            return Ok(new { text, timestamp = DateTime.UtcNow });
        }
        catch (InvalidOperationException ex)
        {
            Log.Warning(ex, "Text generation failed - Invalid operation (IP: {UserIP}, RequestId: {RequestId})", 
                ctx.UserIP, ctx.RequestId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Text generation is currently unavailable" });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Text generation failed - Unexpected error (IP: {UserIP}, RequestId: {RequestId})", 
                ctx.UserIP, ctx.RequestId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An unexpected error occurred" });
        }
    }
}