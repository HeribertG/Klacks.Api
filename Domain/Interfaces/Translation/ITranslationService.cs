namespace Klacks.Api.Domain.Interfaces.Translation;

public interface ITranslationService
{
    Task<TranslationResult> TranslateAsync(string text, string sourceLanguage, string targetLanguage);
    Task<Dictionary<string, string>> TranslateToAllLanguagesAsync(string text, string sourceLanguage);
    bool IsConfigured { get; }
}

public record TranslationResult(string TranslatedText, string SourceLanguage, string TargetLanguage);
