// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The localized texts of the inbound clarification dialog. The four core languages live in the
/// per-language tables of this namespace (German, English, French and Italian ClarificationTexts), the 21
/// plugin languages are merged in at startup from each pack's assistant-texts.json by
/// AssistantTextsPluginLoader. Resolution is LocalizedTextCatalogue's: English only for a tag that no core
/// table and no pack claims, never for an installed language, whose missing key resolves to nothing - and
/// cannot ship, because the pack coverage guard fails on it.
/// </summary>

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Constants;

public static class ClarificationTexts
{
    private const string GermanCode = "de";
    private const string EnglishCode = "en";
    private const string FrenchCode = "fr";
    private const string ItalianCode = "it";

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> CoreTable = BuildCoreTable();

    private static readonly LocalizedTextCatalogue Catalogue = new(CoreTable);

    /// <summary>
    /// The languages whose texts are authored in code rather than shipped by a pack: MultiLanguage.CoreLanguages,
    /// so a fifth core language cannot be added without the coverage guard going red.
    /// </summary>
    public static readonly IReadOnlyList<string> CoreLanguages = MultiLanguage.CoreLanguages;

    /// <summary>
    /// Adds (or replaces) the texts of one language pack. Called once per pack at startup by
    /// AssistantTextsPluginLoader.
    /// </summary>
    /// <param name="languageCode">Locale of the pack the texts were read from</param>
    /// <param name="texts">The pack's texts, keyed by the keys of ClarificationTextKeys</param>
    public static void Configure(string languageCode, IReadOnlyDictionary<string, string> texts) =>
        Catalogue.Configure(languageCode, texts);

    /// <summary>
    /// Resolves one text for a language. False only when an installed language lacks the key; an unknown
    /// language resolves to English.
    /// </summary>
    /// <param name="key">One of ClarificationTextKeys</param>
    /// <param name="language">Installation language, possibly regional (de-CH), or null</param>
    /// <param name="text">The resolved template, empty when nothing resolves</param>
    public static bool TryGetText(string key, string? language, out string text) =>
        Catalogue.TryGetText(key, language, out text);

    /// <summary>
    /// The English text of a key: the wording skill outputs for the language model use, and the floor when
    /// an installed language lacks a key.
    /// </summary>
    /// <param name="key">One of ClarificationTextKeys</param>
    public static string English(string key) => EnglishClarificationTexts.Texts[key];

    /// <summary>Every key of the core catalogue, for the guard tests.</summary>
    internal static IReadOnlyCollection<string> Keys => Catalogue.Keys;

    /// <summary>The per-language core variants of one key, for the guard tests.</summary>
    /// <param name="key">One of ClarificationTextKeys</param>
    internal static IReadOnlyDictionary<string, string> VariantsOf(string key) => Catalogue.VariantsOf(key);

    /// <summary>Discards every configured pack. Test-only, see LocalizedTextCatalogue.Reset.</summary>
    internal static void Reset() => Catalogue.Reset();

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> BuildCoreTable()
    {
        var byLanguage = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            [GermanCode] = GermanClarificationTexts.Texts,
            [EnglishCode] = EnglishClarificationTexts.Texts,
            [FrenchCode] = FrenchClarificationTexts.Texts,
            [ItalianCode] = ItalianClarificationTexts.Texts
        };

        return ClarificationTextKeys.RequiredKeys.ToDictionary(
            key => key,
            key => (IReadOnlyDictionary<string, string>)byLanguage.ToDictionary(
                pair => pair.Key, pair => pair.Value[key], StringComparer.OrdinalIgnoreCase),
            StringComparer.Ordinal);
    }
}
