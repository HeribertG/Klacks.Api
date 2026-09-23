// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Appends the commissioning-help hint to a plugin install/enable result message, so the chat path offers
/// the same help as the UI path. The plugin is re-read after the state change; the hint is only added when
/// its manifest declares an assistant setup and it is not operational yet. The trigger phrase is taken from
/// the plugin i18n in the user's language: the full tag first (keeps "zh-TW"), then its base language
/// (turns "de-CH" into "de"), then English.
/// </summary>
/// <param name="featurePluginService">Source of the plugin state and the plugin translations</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Application.Services.Plugins;

public class FeaturePluginAssistantSetupHintService : IFeaturePluginAssistantSetupHintService
{
    private readonly IFeaturePluginService _featurePluginService;

    public FeaturePluginAssistantSetupHintService(IFeaturePluginService featurePluginService)
    {
        _featurePluginService = featurePluginService;
    }

    public async Task<string> AppendHintAsync(string pluginName, string message, string? userLanguage)
    {
        var plugin = await _featurePluginService.GetPluginAsync(pluginName);
        if (plugin?.AssistantSetup == null || plugin.IsOperational)
        {
            return message;
        }

        var triggerPhrase = ResolveTriggerPhrase(plugin.AssistantSetup.TriggerPhraseKey, userLanguage);
        if (string.IsNullOrWhiteSpace(triggerPhrase))
        {
            return message;
        }

        return message + string.Format(FeaturePluginConstants.AssistantSetupHintFormat, triggerPhrase);
    }

    private string? ResolveTriggerPhrase(string triggerPhraseKey, string? userLanguage)
    {
        var fallbackPhrase = Lookup(FeaturePluginConstants.I18nFallbackLanguage, triggerPhraseKey);

        foreach (var candidate in CandidateLanguages(userLanguage))
        {
            var phrase = Lookup(candidate, triggerPhraseKey);
            if (!string.IsNullOrWhiteSpace(phrase) && !string.Equals(phrase, fallbackPhrase, StringComparison.Ordinal))
            {
                return phrase;
            }
        }

        return fallbackPhrase;
    }

    private string? Lookup(string language, string key)
    {
        var translations = _featurePluginService.GetTranslations(language);
        return translations != null && translations.TryGetValue(key, out var value) ? value : null;
    }

    private static IEnumerable<string> CandidateLanguages(string? userLanguage)
    {
        if (string.IsNullOrWhiteSpace(userLanguage))
        {
            yield break;
        }

        var fullTag = userLanguage.Trim();
        yield return fullTag;

        var baseLanguage = LanguageTag.BaseLanguage(fullTag);
        if (!string.IsNullOrWhiteSpace(baseLanguage) && !string.Equals(baseLanguage, fullTag, StringComparison.OrdinalIgnoreCase))
        {
            yield return baseLanguage;
        }
    }
}
