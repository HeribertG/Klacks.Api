using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Infrastructure.Services.Translation;

public class MultiLanguageTranslationService : IMultiLanguageTranslationService
{
    private readonly ITranslationService _translationService;

    public MultiLanguageTranslationService(ITranslationService translationService)
    {
        _translationService = translationService;
    }

    public bool IsConfigured => _translationService.IsConfigured;

    public async Task<MultiLanguage> TranslateEmptyFieldsAsync(MultiLanguage multiLanguage)
    {
        if (!IsConfigured || multiLanguage == null)
        {
            return multiLanguage ?? new MultiLanguage();
        }

        var (sourceLanguage, sourceText) = FindSourceLanguageAndText(multiLanguage);
        if (sourceLanguage == null || string.IsNullOrWhiteSpace(sourceText))
        {
            return multiLanguage;
        }

        var emptyLanguages = GetEmptyLanguages(multiLanguage);
        if (emptyLanguages.Count == 0)
        {
            return multiLanguage;
        }

        var translations = await _translationService.TranslateToAllLanguagesAsync(sourceText, sourceLanguage);

        return ApplyTranslations(multiLanguage, translations, emptyLanguages);
    }

    private static (string? language, string? text) FindSourceLanguageAndText(MultiLanguage multiLanguage)
    {
        foreach (var language in MultiLanguage.SupportedLanguages)
        {
            var value = multiLanguage.GetValue(language);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return (language, value);
            }
        }
        return (null, null);
    }

    private static List<string> GetEmptyLanguages(MultiLanguage multiLanguage)
    {
        return MultiLanguage.SupportedLanguages
            .Where(lang => string.IsNullOrWhiteSpace(multiLanguage.GetValue(lang)))
            .ToList();
    }

    private static MultiLanguage ApplyTranslations(
        MultiLanguage original,
        Dictionary<string, string> translations,
        List<string> emptyLanguages)
    {
        var result = new MultiLanguage();

        foreach (var language in MultiLanguage.SupportedLanguages)
        {
            result.SetValue(language, original.GetValue(language));
        }

        foreach (var language in emptyLanguages)
        {
            if (translations.TryGetValue(language, out var translation))
            {
                result.SetValue(language, translation);
            }
        }

        return result;
    }
}
