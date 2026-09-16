// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The one user-facing sentence of the correction path that bypasses the model entirely: the two-option
/// clarification of design rule 2. Because no model renders it, it is authored per language - and per
/// the owner's one-language rule (spec §1 rule 4) an installed language never gets an English
/// substitute. The four core languages live here; the 21 plugin languages are merged in at startup from
/// each pack's assistant-texts.json by AssistantTextsPluginLoader, through the same additive
/// Configure/Reset pattern the conversation-signal detectors use.
/// English remains only for a tag that no pack claims - an unknown language, never an installed one. An
/// installed language whose pack lacks the key resolves to nothing, so the turn falls back to the
/// ordinary note path rather than asking in the wrong language; that state cannot ship, because
/// AssistantTextsPackCoverageTests fails on a missing key.
/// The sentence carries rule 1 itself: it names the misunderstanding ({previousAction}) before offering
/// the two options, so even a clarification turn tells the user what was understood wrongly.
/// Placeholders are named rather than positional, so a translator can reorder them for a language whose
/// syntax demands it without changing their meaning.
/// </summary>

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Constants;

public static class GracefulCorrectionTexts
{
    private const string German = "de";
    private const string English = "en";
    private const string French = "fr";
    private const string Italian = "it";

    public const string ClarificationQuestion = "assistant.correction.clarificationQuestion";

    public const string PreviousActionPlaceholder = "{previousAction}";
    public const string FirstOptionPlaceholder = "{optionA}";
    public const string SecondOptionPlaceholder = "{optionB}";

    /// <summary>
    /// The languages whose sentence is authored in this file rather than shipped by a pack. Not a list of
    /// its own: it is MultiLanguage.CoreLanguages, so a fifth core language cannot be added to the
    /// application without this catalogue going red in the coverage guard.
    /// </summary>
    public static readonly IReadOnlyList<string> CoreLanguages = MultiLanguage.CoreLanguages;

    /// <summary>Every key a language pack has to ship. The coverage guard reads exactly this list.</summary>
    public static readonly IReadOnlyList<string> RequiredKeys = [ClarificationQuestion];

    /// <summary>Every placeholder each key must contain, in every language.</summary>
    public static readonly IReadOnlyList<string> RequiredPlaceholders =
        [PreviousActionPlaceholder, FirstOptionPlaceholder, SecondOptionPlaceholder];

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> CoreTexts =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
        {
            [ClarificationQuestion] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "Verstanden — nicht {previousAction}. Meinst du {optionA} oder {optionB}?",
                [English] = "Understood — not {previousAction}. Do you mean {optionA} or {optionB}?",
                [French] = "Compris — pas {previousAction}. Veux-tu dire {optionA} ou {optionB} ?",
                [Italian] = "Capito — non {previousAction}. Intendi {optionA} o {optionB}?"
            }
        };

    private static readonly object ConfigureLock = new();

    private static Dictionary<string, IReadOnlyDictionary<string, string>> _pluginTexts =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds (or replaces) the texts of one language pack. Called once per pack at startup by
    /// AssistantTextsPluginLoader. Additive across languages, replacing within one language: a pack is
    /// installed or uninstalled as a unit, and a half-updated language is worse than a missing one.
    /// </summary>
    /// <param name="languageCode">Locale of the pack the texts were read from</param>
    /// <param name="texts">The pack's texts, keyed by the catalogue keys of RequiredKeys</param>
    public static void Configure(string languageCode, IReadOnlyDictionary<string, string> texts)
    {
        if (string.IsNullOrWhiteSpace(languageCode) || texts.Count == 0)
        {
            return;
        }

        lock (ConfigureLock)
        {
            _pluginTexts = new Dictionary<string, IReadOnlyDictionary<string, string>>(
                _pluginTexts, StringComparer.OrdinalIgnoreCase)
            {
                [languageCode] = new Dictionary<string, string>(texts, StringComparer.OrdinalIgnoreCase)
            };
        }
    }

    /// <summary>
    /// Discards every configured pack and restores the core-only state. Test-only: Configure writes
    /// process-wide static state, so without a way back a fixture that loads a pack would decide the
    /// outcome of every fixture running after it.
    /// </summary>
    internal static void Reset()
    {
        lock (ConfigureLock)
        {
            _pluginTexts = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Resolves a text for a language. Core languages come from the table above, installed plugin
    /// languages from their pack, and ONLY an unknown tag falls back to English. An installed language
    /// whose pack lacks the key returns false - the caller then asks nothing rather than in the wrong
    /// language.
    /// A regional tag is tried in full first and only then reduced to its base language, so "zh-CN" and
    /// "zh-TW" keep their own packs while "de-CH" reaches German and "pt-BR" the Portuguese pack. Without
    /// that second probe every region-qualified installation would land on the English fallback, which is
    /// precisely what the one-language rule forbids.
    /// </summary>
    /// <param name="key">Catalogue key of the wanted sentence</param>
    /// <param name="language">Active language of the turn, or null when the turn carries none</param>
    /// <param name="text">The resolved sentence, empty when nothing resolves</param>
    public static bool TryGetText(string key, string? language, out string text)
    {
        text = string.Empty;
        if (string.IsNullOrWhiteSpace(key) || !CoreTexts.TryGetValue(key, out var byLanguage))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(language))
        {
            var exact = ClaimedBy(byLanguage, key, language!, out text);
            if (exact.HasValue)
            {
                return exact.Value;
            }

            var baseLanguage = LanguageTag.BaseLanguage(language);
            if (!string.IsNullOrWhiteSpace(baseLanguage)
                && !string.Equals(baseLanguage, language, StringComparison.OrdinalIgnoreCase))
            {
                var byBase = ClaimedBy(byLanguage, key, baseLanguage!, out text);
                if (byBase.HasValue)
                {
                    return byBase.Value;
                }
            }
        }

        if (byLanguage.TryGetValue(LanguageConfig.DefaultLanguageFallback, out var fallback))
        {
            text = fallback;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Whether one exact language tag owns this text, as a three-way answer: true with the text when it
    /// does, false when the tag belongs to an installed pack that is missing the key (rule 4 - the caller
    /// asks nothing rather than in the wrong language, and no further lookup may rescue it), and null
    /// when the tag claims nothing at all, which is the only case the caller may keep searching after.
    /// </summary>
    /// <param name="byLanguage">The core table of this key</param>
    /// <param name="key">Catalogue key of the wanted sentence</param>
    /// <param name="language">One exact language tag, never blank</param>
    /// <param name="text">The resolved sentence, empty unless the answer is true</param>
    private static bool? ClaimedBy(
        IReadOnlyDictionary<string, string> byLanguage, string key, string language, out string text)
    {
        text = string.Empty;

        if (byLanguage.TryGetValue(language, out var core))
        {
            text = core;
            return true;
        }

        if (!_pluginTexts.TryGetValue(language, out var pack))
        {
            return null;
        }

        if (!pack.TryGetValue(key, out var localized) || string.IsNullOrWhiteSpace(localized))
        {
            return false;
        }

        text = localized;
        return true;
    }

    /// <summary>Every key of the core catalogue, for the completeness guard test.</summary>
    internal static IReadOnlyCollection<string> Keys => CoreTexts.Keys.ToList();

    /// <summary>The per-language variants of one core key, for the completeness guard test.</summary>
    /// <param name="key">Catalogue key whose per-language table is wanted</param>
    internal static IReadOnlyDictionary<string, string> VariantsOf(string key) =>
        CoreTexts.TryGetValue(key, out var byLanguage)
            ? byLanguage
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
