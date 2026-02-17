namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IPromptTranslationProvider
{
    Task<Dictionary<string, string>> GetTranslationsAsync(string language);
}
