// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The label of the "explain briefly first" chip Klacksy attaches to a gate. The chip is sent back as the user's
/// next message, so the label doubles as the phrase GateExplainOfferService recognizes as accepting the offer -
/// in any language, via AllTextsOf. The four core languages live in the table below, the 21 plugin languages are
/// merged in at startup from each pack's assistant-texts.json by AssistantTextsPluginLoader (the key IS the pack
/// key). Resolution is LocalizedTextCatalogue's; a language without a text gets the English label.
/// </summary>

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Constants;

public static class ExplainOfferTexts
{
    private const string German = "de";
    private const string English = "en";
    private const string French = "fr";
    private const string Italian = "it";

    public const string ChipLabel = "assistant.explainOffer.chipLabel";

    /// <summary>Every key a language pack has to ship in assistant-texts.json. The catalogue guard reads exactly this list.</summary>
    public static readonly IReadOnlyList<string> RequiredKeys = [ChipLabel];

    /// <summary>The languages whose texts are authored in code rather than shipped by a pack.</summary>
    public static readonly IReadOnlyList<string> CoreLanguages = MultiLanguage.CoreLanguages;

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> CoreTexts =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
        {
            [ChipLabel] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "Erst kurz erklären",
                [English] = "Explain briefly first",
                [French] = "D'abord une courte explication",
                [Italian] = "Innanzitutto una breve spiegazione"
            }
        };

    private static readonly LocalizedTextCatalogue Catalogue = new(CoreTexts);

    /// <summary>
    /// Adds (or replaces) the texts of one language pack. Called once per pack at startup by
    /// AssistantTextsPluginLoader.
    /// </summary>
    /// <param name="languageCode">Locale of the pack the texts were read from</param>
    /// <param name="texts">The pack's texts, keyed by the keys of RequiredKeys</param>
    public static void Configure(string languageCode, IReadOnlyDictionary<string, string> texts) =>
        Catalogue.Configure(languageCode, texts);

    /// <summary>Resolves the text for a language; only an unknown language falls back to English.</summary>
    /// <param name="key">One of RequiredKeys</param>
    /// <param name="language">Language tag, possibly regional (de-CH), or null</param>
    /// <param name="text">The resolved text, empty when nothing resolves</param>
    public static bool TryGetText(string key, string? language, out string text) =>
        Catalogue.TryGetText(key, language, out text);

    /// <summary>
    /// Resolves the text only when the language (full tag or base language) claims it; never English by fallback.
    /// </summary>
    /// <param name="key">One of RequiredKeys</param>
    /// <param name="language">Language tag, possibly regional (de-CH), or null</param>
    /// <param name="text">The resolved text, empty when the language does not claim the key</param>
    public static bool TryGetOwnText(string key, string? language, out string text) =>
        Catalogue.TryGetOwnText(key, language, out text);

    /// <summary>Every text one key currently resolves to in any language, to recognize an accepted offer whatever the language.</summary>
    /// <param name="key">One of RequiredKeys</param>
    public static IReadOnlyCollection<string> AllTextsOf(string key) => Catalogue.AllTextsOf(key);

    /// <summary>The English text of a key.</summary>
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
