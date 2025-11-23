namespace PoFastType.Api.Services;

public interface ITextGenerationService
{
    Task<string> GenerateTextAsync();
}