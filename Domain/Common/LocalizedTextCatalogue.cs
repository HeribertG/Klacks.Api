// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Language resolution shared by the catalogues of user-facing sentences that reach the user without a
/// model call (GracefulCorrectionTexts, ClarificationTexts). The four core languages come from a table
/// authored in code, the installed plugin languages from their pack, merged in at startup through the
/// additive Configure/Reset pattern. English is the fallback for a tag that no core table and no configured
/// pack claims - an unknown language. A language whose configured pack lacks a key resolves to nothing, so
/// the caller decides what to do rather than the catalogue silently speaking English. "Configured" is
/// narrower than "installed": a pack whose texts were never passed to Configure (a directory without
/// assistant-texts.json, or one installed after startup) is indistinguishable from an unknown language here
/// and resolves to English. A regional tag is tried
/// in full first and only then reduced to its base language, so "zh-CN" and "zh-TW" keep their own packs
/// while "de-CH" reaches German and "pt-BR" the Portuguese pack.
/// </summary>
/// <param name="coreTexts">The core-language table: catalogue key, then language code, then sentence</param>

namespace Klacks.Api.Domain.Common;

public sealed class LocalizedTextCatalogue
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _coreTexts;
    private readonly object _configureLock = new();
    private volatile Dictionary<string, IReadOnlyDictionary<string, string>> _pluginTexts =
        new(StringComparer.OrdinalIgnoreCase);

    public LocalizedTextCatalogue(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> coreTexts)
    {
        _coreTexts = coreTexts;
    }

    /// <summary>Every key of the core table.</summary>
    public IReadOnlyCollection<string> Keys => _coreTexts.Keys.ToList();

    /// <summary>
    /// Adds (or replaces) the texts of one language pack. Additive across languages, replacing within one
    /// language: a pack is installed or uninstalled as a unit, and a half-updated language is worse than a
    /// missing one.
    /// </summary>
    /// <param name="languageCode">Locale of the pack the texts were read from</param>
    /// <param name="texts">The pack's texts, keyed by catalogue key</param>
    public void Configure(string languageCode, IReadOnlyDictionary<string, string> texts)
    {
        if (string.IsNullOrWhiteSpace(languageCode) || texts.Count == 0)
        {
            return;
        }

        lock (_configureLock)
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
    /// process-wide state, so without a way back a fixture that loads a pack would decide the outcome of
    /// every fixture running after it.
    /// </summary>
    internal void Reset()
    {
        lock (_configureLock)
        {
            _pluginTexts = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Resolves a text for a language. Core languages come from the table, installed plugin languages from
    /// their configured pack, and ONLY a tag that no table and no configured pack claims falls back to
    /// English. A language whose configured pack lacks the key returns false.
    /// </summary>
    /// <param name="key">Catalogue key of the wanted sentence</param>
    /// <param name="language">Active language, or null when there is none</param>
    /// <param name="text">The resolved sentence, empty when nothing resolves</param>
    public bool TryGetText(string key, string? language, out string text)
    {
        text = string.Empty;
        if (string.IsNullOrWhiteSpace(key) || !_coreTexts.TryGetValue(key, out var byLanguage))
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
    /// Every text one key currently resolves to in any language: the core table plus every configured pack.
    /// </summary>
    /// <param name="key">Catalogue key whose texts are wanted</param>
    public IReadOnlyCollection<string> AllTextsOf(string key)
    {
        var packs = _pluginTexts;
        return VariantsOf(key).Values
            .Concat(packs.Values.Select(pack => pack.GetValueOrDefault(key)).OfType<string>())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>The per-language core variants of one key.</summary>
    /// <param name="key">Catalogue key whose per-language table is wanted</param>
    public IReadOnlyDictionary<string, string> VariantsOf(string key) =>
        _coreTexts.TryGetValue(key, out var byLanguage)
            ? byLanguage
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether one exact language tag owns this text, as a three-way answer: true with the text when it
    /// does, false when the tag belongs to an installed pack that is missing the key (no further lookup may
    /// rescue it), and null when the tag claims nothing at all, which is the only case the caller may keep
    /// searching after.
    /// </summary>
    /// <param name="byLanguage">The core table of this key</param>
    /// <param name="key">Catalogue key of the wanted sentence</param>
    /// <param name="language">One exact language tag, never blank</param>
    /// <param name="text">The resolved sentence, empty unless the answer is true</param>
    private bool? ClaimedBy(
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
}
