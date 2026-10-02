// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The name prefixes of the scenarios the server creates on its own (wizard, harmonizer, optimizer, AutoWizard
/// stages, plan proposals and absence covers). The name is written once when the scenario is created and is
/// shown as-is in every view afterwards, so the prefix has to be in the planner's language at that moment.
/// The four core languages live in the table below, the 21 plugin languages are merged in at startup from each
/// pack's assistant-texts.json by AssistantTextsPluginLoader (the key IS the pack key). Resolution is
/// LocalizedTextCatalogue's; TryGetOwnText additionally lets the name generator skip a language nobody claims
/// and fall back to the installation language instead of to English.
/// </summary>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Constants;

public static class ScenarioNameTexts
{
    private const string German = "de";
    private const string English = "en";
    private const string French = "fr";
    private const string Italian = "it";

    private const string Prefix = "assistant.scenarioName.";

    public const string AutoPlan = Prefix + "autoPlan";
    public const string AutoHarmonizer = Prefix + "autoHarmonizer";
    public const string Auto = Prefix + "auto";
    public const string Plan = Prefix + "plan";
    public const string Harmonized = Prefix + "harmonized";
    public const string Llm = Prefix + "llm";
    public const string Optimizer = Prefix + "optimizer";
    public const string Proposal = Prefix + "proposal";
    public const string AbsenceCover = Prefix + "absenceCover";

    /// <summary>Every key a language pack has to ship in assistant-texts.json. The catalogue guard reads exactly this list.</summary>
    public static readonly IReadOnlyList<string> RequiredKeys =
    [
        AutoPlan, AutoHarmonizer, Auto, Plan, Harmonized, Llm, Optimizer, Proposal, AbsenceCover
    ];

    /// <summary>The languages whose texts are authored in code rather than shipped by a pack.</summary>
    public static readonly IReadOnlyList<string> CoreLanguages = MultiLanguage.CoreLanguages;

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> CoreTexts =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
        {
            [AutoPlan] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "Auto-Erstellung Plan",
                [English] = "Auto-created plan",
                [French] = "Plan auto-généré",
                [Italian] = "Piano auto-generato"
            },
            [AutoHarmonizer] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "Auto-Erstellung Harmonizer",
                [English] = "Auto-created harmonized plan",
                [French] = "Plan harmonisé auto-généré",
                [Italian] = "Piano armonizzato auto-generato"
            },
            [Auto] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "Auto-Erstellung",
                [English] = "Auto-created",
                [French] = "Auto-généré",
                [Italian] = "Auto-generato"
            },
            [Plan] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "Plan",
                [English] = "Plan",
                [French] = "Plan",
                [Italian] = "Piano"
            },
            [Harmonized] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "Harmonisiert",
                [English] = "Harmonized",
                [French] = "Harmonisé",
                [Italian] = "Armonizzato"
            },
            [Llm] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "KI-Plan",
                [English] = "AI plan",
                [French] = "Plan IA",
                [Italian] = "Piano IA"
            },
            [Optimizer] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "Optimiert",
                [English] = "Optimized",
                [French] = "Optimisé",
                [Italian] = "Ottimizzato"
            },
            [Proposal] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "Vorschlag",
                [English] = "Proposal",
                [French] = "Proposition",
                [Italian] = "Proposta"
            },
            [AbsenceCover] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "Abwesenheitsvertretung",
                [English] = "Absence cover",
                [French] = "Remplacement d'absence",
                [Italian] = "Sostituzione per assenza"
            }
        };

    private static readonly LocalizedTextCatalogue Catalogue = new(CoreTexts);

    /// <summary>The catalogue key of one scenario name kind.</summary>
    /// <param name="kind">Which server-side process creates the scenario</param>
    public static string KeyOf(ScenarioNameKind kind) => kind switch
    {
        ScenarioNameKind.AutoPlan => AutoPlan,
        ScenarioNameKind.AutoHarmonizer => AutoHarmonizer,
        ScenarioNameKind.Auto => Auto,
        ScenarioNameKind.Plan => Plan,
        ScenarioNameKind.Harmonized => Harmonized,
        ScenarioNameKind.Llm => Llm,
        ScenarioNameKind.Optimizer => Optimizer,
        ScenarioNameKind.Proposal => Proposal,
        ScenarioNameKind.AbsenceCover => AbsenceCover,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown scenario name kind.")
    };

    /// <summary>
    /// Adds (or replaces) the texts of one language pack. Called once per pack at startup by
    /// AssistantTextsPluginLoader.
    /// </summary>
    /// <param name="languageCode">Locale of the pack the texts were read from</param>
    /// <param name="texts">The pack's texts, keyed by the keys of RequiredKeys</param>
    public static void Configure(string languageCode, IReadOnlyDictionary<string, string> texts) =>
        Catalogue.Configure(languageCode, texts);

    /// <summary>
    /// Resolves one text for a language with the catalogue's English fallback for an unknown language.
    /// </summary>
    /// <param name="key">One of RequiredKeys</param>
    /// <param name="language">Language tag, possibly regional (de-CH), or null</param>
    /// <param name="text">The resolved text, empty when nothing resolves</param>
    public static bool TryGetText(string key, string? language, out string text) =>
        Catalogue.TryGetText(key, language, out text);

    /// <summary>
    /// Resolves one text only when the language (full tag or base language) claims it; never English by fallback.
    /// </summary>
    /// <param name="key">One of RequiredKeys</param>
    /// <param name="language">Language tag, possibly regional (de-CH), or null</param>
    /// <param name="text">The resolved text, empty when the language does not claim the key</param>
    public static bool TryGetOwnText(string key, string? language, out string text) =>
        Catalogue.TryGetOwnText(key, language, out text);

    /// <summary>The English text of a key: the floor when a loaded pack lacks a key.</summary>
    /// <param name="key">One of RequiredKeys</param>
    public static string EnglishOf(string key) => CoreTexts[key][LanguageConfig.DefaultLanguageFallback];

    /// <summary>Every key of the core catalogue, for the guard tests.</summary>
    internal static IReadOnlyCollection<string> Keys => Catalogue.Keys;

    /// <summary>The per-language core variants of one key, for the guard tests.</summary>
    /// <param name="key">One of RequiredKeys</param>
    internal static IReadOnlyDictionary<string, string> VariantsOf(string key) => Catalogue.VariantsOf(key);

    /// <summary>Discards every configured pack. Test-only, see LocalizedTextCatalogue.Reset.</summary>
    internal static void Reset() => Catalogue.Reset();
}
