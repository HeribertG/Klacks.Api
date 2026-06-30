// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Constants;

public static class LanguagePluginConstants
{
    public const string PluginDirectory = "Plugins/Languages";
    public const string ManifestFileName = "manifest.json";
    public const string TranslationsFileName = "translations.json";
    public const string DocsDirectory = "docs";
    public const string CountriesFileName = "countries.json";
    public const string StatesFileName = "states.json";
    public const string CalendarRulesFileName = "calendar-rules.json";
    public const string SkillSynonymsFileName = "skill-synonyms.json";
    public const string RecipeSynonymsFileName = "recipe-synonyms.json";
    public const string SentimentKeywordsFileName = "sentiment-keywords.json";
    public const string WakeWordsFileName = "wake-words.json";
    public const string NavigationTargetsFileName = "navigation-targets.json";
    public const string NavigationIntentFileName = "navigation-intent.json";
    public const string MutationIntentFileName = "mutation-intent.json";
    public const string PhoneticsFileName = "phonetics.json";
    public const string PhoneticsCoreFileName = "phonetics-core.json";
    public const string SettingPrefix = "INSTALLED_LANGUAGE_";

    public static readonly string[] CoreLanguages = ["de", "en", "fr", "it"];
}
