namespace PoFastType.Api.Services;

/// <summary>
/// Service interface for generating practice text for typing tests.
/// Follows Single Responsibility Principle - handles only text generation logic.
/// </summary>
public interface ITextGenerationService
{
    /// <summary>
    /// Generates random practice text for a typing test.
    /// </summary>
    /// <returns>A string containing practice text (typically 50-200 words)</returns>
    /// <exception cref="InvalidOperationException">Thrown when text generation fails</exception>
    Task<string> GenerateTextAsync();
}