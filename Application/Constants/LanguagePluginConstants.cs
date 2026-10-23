// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Application.Constants;

public static class LanguagePluginConstants
{
    public const string PluginDirectory = "Plugins/Languages";
    public const string DirectoryConfigKey = "LanguagePlugins:Directory";
    public const string ManifestFileName = "manifest.json";
    public const string TranslationsFileName = "translations.json";
    public const string DocsDirectory = "docs";
    public const string CountriesFileName = "countries.json";
    public const string StatesFileName = "states.json";
    public const string DefaultGeoTranslationsFileName = "default-geo-translations.json";
    public const string DefaultQualificationTranslationsFileName = "default-qualification-translations.json";
    public const string DefaultCalendarRuleTranslationsFileName = "default-calendar-rule-translations.json";
    public const string DefaultAbsenceTranslationsFileName = "default-absence-translations.json";
    public const string CalendarRulesFileName = "calendar-rules.json";
    public const string CalendarRuleLegacyIdsFileName = "calendar-rule-legacy-ids.json";
    public const string CalendarRuleBackfillSettingPrefix = "LANGUAGE_CALENDAR_RULES_BACKFILLED_";
    public const string CalendarRuleBackfillDoneValue = "true";
    public const string SkillSynonymsFileName = "skill-synonyms.json";
    public const string SkillLabelsFileName = "skill-labels.json";
    public const string RecipeSynonymsFileName = "recipe-synonyms.json";
    public const string RecipeVetoesFileName = "recipe-vetoes.json";
    public const string RecipeAnchorsFileName = "recipe-anchors.json";
    public const string SentimentKeywordsFileName = "sentiment-keywords.json";
    public const string WakeWordsFileName = "wake-words.json";
    public const string NavigationTargetsFileName = "navigation-targets.json";
    public const string NavigationIntentFileName = "navigation-intent.json";
    public const string MutationIntentFileName = "mutation-intent.json";
    public const string GroupingIntentFileName = "grouping-intent.json";
    public const string CompletionClaimFileName = "completion-claim.json";
    public const string ConversationSignalsFileName = "conversation-signals.json";
    public const string ConversationSignalsCoreFileName = "conversation-signals-core.json";
    public const string ApplicationDirectory = "Application";
    public const string KlacksyDirectory = "Klacksy";
    public const string AssistantTextsFileName = "assistant-texts.json";
    public const string PhoneticsFileName = "phonetics.json";
    public const string PhoneticsCoreFileName = "phonetics-core.json";
    public const string SettingPrefix = "INSTALLED_LANGUAGE_";
    public const string StoreBundleFilesProperty = "files";

    /// <summary>
    /// Pack files the store bundle carries under "files" (besides manifest, translations, docs, countries, states and
    /// calendar rules, which have their own bundle keys). Installing a pack from the store writes only these names,
    /// so a bundle can never place a file Klacks does not read. A guard test fails when a pack in Plugins/Languages
    /// ships a file that is neither here nor a bundle key, so new pack files reach store installs too.
    /// </summary>
    public static readonly IReadOnlySet<string> StoreBundleFiles = new HashSet<string>(StringComparer.Ordinal)
    {
        CalendarRuleLegacyIdsFileName,
        SkillSynonymsFileName,
        SkillLabelsFileName,
        RecipeSynonymsFileName,
        RecipeVetoesFileName,
        RecipeAnchorsFileName,
        SentimentKeywordsFileName,
        WakeWordsFileName,
        NavigationTargetsFileName,
        NavigationIntentFileName,
        MutationIntentFileName,
        GroupingIntentFileName,
        CompletionClaimFileName,
        ConversationSignalsFileName,
        AssistantTextsFileName,
        PhoneticsFileName,
    };

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
