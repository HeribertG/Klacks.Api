// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Translation;

public interface ITranslationService
{
    Task<TranslationResult> TranslateAsync(string text, string sourceLanguage, string targetLanguage);
    Task<Dictionary<string, string>> TranslateToAllLanguagesAsync(string text, string sourceLanguage);
    Task<bool> IsConfiguredAsync();
}

public record TranslationResult(string TranslatedText, string SourceLanguage, string TargetLanguage);
