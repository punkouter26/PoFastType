namespace PoFastType.Api.Services;

/// <summary>
/// Strategy Pattern (GoF) - Defines the interface for text generation strategies
/// Open/Closed Principle (SOLID) - Open for extension (new strategies), closed for modification
/// </summary>
public interface ITextGenerationStrategy
{
    Task<string> GenerateTextAsync();
    string StrategyName { get; }
}
