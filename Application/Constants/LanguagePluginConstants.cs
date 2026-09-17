// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Application.Constants;

public static class LanguagePluginConstants
{
    public const string PluginDirectory = "Plugins/Languages";
    public const string ManifestFileName = "manifest.json";
    public const string TranslationsFileName = "translations.json";
    public const string DocsDirectory = "docs";
    public const string CountriesFileName = "countries.json";
    public const string StatesFileName = "states.json";
    public const string DefaultGeoTranslationsFileName = "default-geo-translations.json";
    public const string CalendarRulesFileName = "calendar-rules.json";
    public const string SkillSynonymsFileName = "skill-synonyms.json";
    public const string SkillLabelsFileName = "skill-labels.json";
    public const string RecipeSynonymsFileName = "recipe-synonyms.json";
    public const string RecipeVetoesFileName = "recipe-vetoes.json";
    public const string SentimentKeywordsFileName = "sentiment-keywords.json";
    public const string WakeWordsFileName = "wake-words.json";
    public const string NavigationTargetsFileName = "navigation-targets.json";
    public const string NavigationIntentFileName = "navigation-intent.json";
    public const string MutationIntentFileName = "mutation-intent.json";
    public const string GroupingIntentFileName = "grouping-intent.json";
    public const string CompletionClaimFileName = "completion-claim.json";
    public const string ConversationSignalsFileName = "conversation-signals.json";
    public const string AssistantTextsFileName = "assistant-texts.json";
    public const string PhoneticsFileName = "phonetics.json";
    public const string PhoneticsCoreFileName = "phonetics-core.json";
    public const string SettingPrefix = "INSTALLED_LANGUAGE_";

    /// <summary>
    /// The languages Klacks ships in itself, i.e. the ones a language plugin never supplies. Not a list
    /// of its own: it is MultiLanguage.CoreLanguages, because a core language a loader does not know
    /// would let a plugin overwrite what the application ships, while a core language the correction
    /// catalogue does not know would resolve its user-facing question to English and break the
    /// one-language rule. The list lives in Domain because Application may depend on Domain and not the
    /// other way round. Two deliberate copies of the same four codes remain: LanguageConfig
    /// .SupportedLanguages, which is runtime-configurable and therefore not a constant, and the literal
    /// list in migration 20260729060051_AddSkillPhrase, which is a frozen snapshot on purpose.
    /// </summary>
    public static readonly IReadOnlyList<string> CoreLanguages = MultiLanguage.CoreLanguages;
}
