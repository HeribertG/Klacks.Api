// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Translation;

public class MultiLanguageTranslationService : IMultiLanguageTranslationService
{
    private readonly ITranslationService _translationService;
    private readonly ILogger<MultiLanguageTranslationService>? _logger;

    public MultiLanguageTranslationService(ITranslationService translationService, ILogger<MultiLanguageTranslationService>? logger = null)
    {
        _translationService = translationService;
        _logger = logger;
    }

    public async Task<bool> IsConfiguredAsync() => await _translationService.IsConfiguredAsync();

    public async Task<MultiLanguage> TranslateEmptyFieldsAsync(MultiLanguage multiLanguage)
    {
        if (!await IsConfiguredAsync() || multiLanguage == null)
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

        try
        {
            var translations = await _translationService.TranslateToAllLanguagesAsync(sourceText, sourceLanguage);
            return ApplyTranslations(multiLanguage, translations, emptyLanguages);
        }
        catch (TranslationAuthenticationException ex)
        {
            _logger?.LogWarning(ex, "Translation skipped due to provider authentication failure; returning untranslated fields");
            return multiLanguage;
        }
    }

    private static (string? language, string? text) FindSourceLanguageAndText(MultiLanguage multiLanguage)
    {
        foreach (var language in LanguageConfig.SupportedLanguages)
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
        return LanguageConfig.SupportedLanguages
            .Where(lang => string.IsNullOrWhiteSpace(multiLanguage.GetValue(lang)))
            .ToList();
    }

    private static MultiLanguage ApplyTranslations(
        MultiLanguage original,
        Dictionary<string, string> translations,
        List<string> emptyLanguages)
    {
        var result = new MultiLanguage();

        foreach (var language in LanguageConfig.SupportedLanguages)
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
