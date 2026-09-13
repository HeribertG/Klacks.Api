// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves which cultures an ambiguous user-supplied date may be read with, so that "03/04/2026"
/// means 4 March for an English user and 3 April for a French one instead of whatever the server's
/// process culture happens to be. <see cref="CulturesFor"/> answers that per UI language;
/// <see cref="AllSupportedCultures"/> is the union every language can be read with, for the
/// dispatch-time parameter gate that has no language of its own. Used by the skill-level date parsing
/// and by that gate, so the two never disagree about what a user may write.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Services.Assistant.Skills;

public static class SkillDateCultureResolver
{
    private const string ThaiLanguageCode = "th";
    private const string ThaiCultureName = "th-TH";

    /// <summary>
    /// The historical culture list, used for a null, blank or unknown language so every caller that
    /// has no language keeps its previous behaviour.
    /// </summary>
    private static readonly CultureInfo[] FallbackCultures =
    {
        new("de-CH"), new("de-DE"), new("fr-CH"), new("it-CH"),
        CultureInfo.InvariantCulture, new("en-US")
    };

    /// <summary>
    /// One or more culture names per supported UI language, most specific first. Every entry was
    /// verified to use the Gregorian calendar: ar maps to ar-EG rather than ar-SA, whose Umm al-Qura
    /// calendar rejects both "2026-03-04" and "12.09.2026". Thai is the one language whose own culture
    /// is not Gregorian and is therefore built separately by <see cref="BuildThaiCultures"/>.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string[]> LanguageCultureNames =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["ar"] = ["ar-EG"],
            ["cs"] = ["cs-CZ"],
            ["da"] = ["da-DK"],
            ["de"] = ["de-CH", "de-DE"],
            ["el"] = ["el-GR"],
            ["en"] = ["en-US", "en-GB"],
            ["es"] = ["es-ES"],
            ["fi"] = ["fi-FI"],
            ["fr"] = ["fr-CH", "fr-FR"],
            ["he"] = ["he-IL"],
            ["id"] = ["id-ID"],
            ["it"] = ["it-CH", "it-IT"],
            ["ja"] = ["ja-JP"],
            ["ko"] = ["ko-KR"],
            ["ms"] = ["ms-MY"],
            ["nb"] = ["nb-NO"],
            ["nl"] = ["nl-NL"],
            ["pl"] = ["pl-PL"],
            ["pt"] = ["pt-BR", "pt-PT"],
            ["ro"] = ["ro-RO"],
            ["sv"] = ["sv-SE"],
            ["vi"] = ["vi-VN"],
            ["zh-CN"] = ["zh-CN"],
            ["zh-TW"] = ["zh-TW"]
        };

    private static readonly CultureInfo[] ThaiCultures = BuildThaiCultures();

    /// <summary>
    /// Every culture any supported language can be read with, in one list. Used by the dispatch-time
    /// parameter gate, which has no language of its own: the gate must accept at least everything the
    /// language-aware parser accepts, otherwise a Japanese user's "2026/03/04" is rejected before the
    /// skill - and before the parser - ever sees it.
    /// </summary>
    private static readonly CultureInfo[] AllCultures = BuildAllCultures();

    public static IReadOnlyList<CultureInfo> AllSupportedCultures => AllCultures;

    /// <summary>
    /// Cultures to try for a user writing in <paramref name="language"/>, most specific first and with
    /// InvariantCulture last. A null, blank or unknown language falls back to the historical culture
    /// list, which keeps every existing caller on its previous behaviour.
    /// </summary>
    /// <param name="language">UI language code such as "de", "en", "pt" or "zh-CN"; a regional tag
    /// whose base language is known (e.g. "en-GB") resolves through that base language.</param>
    public static IReadOnlyList<CultureInfo> CulturesFor(string? language)
    {
        var normalized = Normalize(language);
        if (normalized is null)
        {
            return FallbackCultures;
        }

        if (string.Equals(normalized, ThaiLanguageCode, StringComparison.OrdinalIgnoreCase))
        {
            return ThaiCultures;
        }

        if (!LanguageCultureNames.TryGetValue(normalized, out var cultureNames))
        {
            return FallbackCultures;
        }

        var resolved = new List<CultureInfo>(cultureNames.Length + 1);
        foreach (var cultureName in cultureNames)
        {
            var culture = TryResolve(cultureName);
            if (culture is not null)
            {
                resolved.Add(culture);
            }
        }

        resolved.Add(CultureInfo.InvariantCulture);
        return resolved;
    }

    private static string? Normalize(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        var trimmed = language.Trim();
        if (LanguageCultureNames.ContainsKey(trimmed) ||
            string.Equals(trimmed, ThaiLanguageCode, StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return LanguageTag.BaseLanguage(trimmed);
    }

    private static CultureInfo[] BuildAllCultures()
    {
        var all = new List<CultureInfo>(FallbackCultures);
        all.AddRange(ThaiCultures);
        foreach (var language in LanguageCultureNames.Keys)
        {
            all.AddRange(CulturesFor(language));
        }

        return all.DistinctBy(culture => (culture.Name, culture.DateTimeFormat.Calendar.GetType())).ToArray();
    }

    /// <summary>
    /// Thai is tried as Gregorian first, then with its own Buddhist calendar. "12.09.2026" is a
    /// Gregorian date and must not become 1483; "12.09.2569" is a Buddhist year and becomes 2026. The
    /// two readings never collide because the caller only accepts a year inside the plausible band, and
    /// a Buddhist year is 543 years above it.
    /// </summary>
    private static CultureInfo[] BuildThaiCultures()
    {
        var buddhist = TryResolve(ThaiCultureName);
        if (buddhist is null)
        {
            return FallbackCultures;
        }

        var cultures = new List<CultureInfo>(3);
        var gregorian = TryBuildGregorianVariant(buddhist);
        if (gregorian is not null)
        {
            cultures.Add(gregorian);
        }

        cultures.Add(buddhist);
        cultures.Add(CultureInfo.InvariantCulture);
        return cultures.ToArray();
    }

    private static CultureInfo? TryBuildGregorianVariant(CultureInfo culture)
    {
        try
        {
            var clone = (CultureInfo)culture.Clone();
            clone.DateTimeFormat.Calendar = new GregorianCalendar();
            return clone;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static CultureInfo? TryResolve(string cultureName)
    {
        try
        {
            return CultureInfo.GetCultureInfo(cultureName);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }
}
