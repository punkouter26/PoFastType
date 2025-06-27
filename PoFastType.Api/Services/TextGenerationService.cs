namespace PoFastType.Api.Services;

/// <summary>
/// Context class for Strategy Pattern (GoF) - Uses different text generation strategies
/// Single Responsibility Principle (SOLID) - Delegates text generation to strategies
/// Dependency Inversion Principle (SOLID) - Depends on ITextGenerationStrategy abstraction
/// </summary>
public class TextGenerationService : ITextGenerationService
{
    private readonly ITextGenerationStrategy _strategy;
    private readonly ILogger<TextGenerationService> _logger;

    public TextGenerationService(ITextGenerationStrategy strategy, ILogger<TextGenerationService> logger) 
    { 
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GenerateTextAsync()
    {
        try
        {
            _logger.LogInformation("Generating text using {Strategy} strategy", _strategy.StrategyName);
            var text = await _strategy.GenerateTextAsync();
            
            if (string.IsNullOrEmpty(text))
            {
                _logger.LogWarning("Text generation strategy returned null or empty text");
                throw new InvalidOperationException("Generated text cannot be null or empty");
            }

            _logger.LogDebug("Successfully generated text of length {Length}", text.Length);
            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating text using {Strategy} strategy", _strategy.StrategyName);
            throw;
        }
    }
}