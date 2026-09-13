// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the culture used to RENDER day and month names for a user, as opposed to the culture list
/// used to READ an ambiguous date. Reuses the verified Gregorian culture mapping of
/// <see cref="Klacks.Api.Domain.Services.Assistant.Skills.SkillDateCultureResolver.CulturesFor"/> but,
/// unlike that list, falls back to <see cref="CultureInfo.InvariantCulture"/> (English names) for a
/// language the mapping does not know: that list ends on the historical Swiss/German default, which
/// would silently render a Russian user's weekday in German. Regional tags are preserved, so "zh-CN"
/// keeps simplified and "zh-TW" traditional Chinese names.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Domain.Common;

public static class UiLanguageCulture
{
    /// <param name="language">UI language code such as "de", "en", "th" or "zh-TW"; null, blank or
    /// unknown resolves to <see cref="CultureInfo.InvariantCulture"/>.</param>
    public static CultureInfo Resolve(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return CultureInfo.InvariantCulture;
        }

        var trimmed = language.Trim();
        var baseLanguage = LanguageTag.BaseLanguage(trimmed);

        foreach (var culture in SkillDateCultureResolver.CulturesFor(trimmed))
        {
            if (!culture.Equals(CultureInfo.InvariantCulture) &&
                string.Equals(culture.TwoLetterISOLanguageName, baseLanguage, StringComparison.OrdinalIgnoreCase))
            {
                return culture;
            }
        }

        return CultureInfo.InvariantCulture;
    }

    /// <param name="language">UI language code of the user the name is rendered for</param>
    /// <param name="dayOfWeek">Weekday to name</param>
    public static string DayName(string? language, DayOfWeek dayOfWeek) =>
        Resolve(language).DateTimeFormat.GetDayName(dayOfWeek);
}
