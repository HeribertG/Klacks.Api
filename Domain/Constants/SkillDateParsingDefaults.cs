// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared vocabulary and cultures for reading user-supplied date parameters in skills:
/// the "today" words a user may send instead of a concrete date, and the cultures whose
/// date formats are accepted. Used by both the dispatch-time parameter type validation
/// and the skill-level date parsing so the two never disagree.
/// </summary>

using System.Globalization;

namespace Klacks.Api.Domain.Constants;

public static class SkillDateParsingDefaults
{
    public static readonly string[] TodayWords =
    {
        "today", "heute", "now", "jetzt", "sofort", "ab sofort", "ab heute",
        "aujourd'hui", "oggi"
    };

    public static readonly CultureInfo[] Cultures =
    {
        new("de-CH"), new("de-DE"), new("fr-CH"), new("it-CH"),
        CultureInfo.InvariantCulture, new("en-US")
    };

    public static bool IsTodayWord(string value) =>
        TodayWords.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);
}
