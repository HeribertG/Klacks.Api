// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The date-parsing constants used when reading user-supplied date parameters in skills: the ISO 8601
/// formats that are always tried first with InvariantCulture, and the band of calendar years a parsed
/// value must fall into to be believable. Which cultures an ambiguous value may additionally be read
/// with is not decided here but by <c>SkillDateCultureResolver</c>. Used by the dispatch-time
/// parameter type validation and by the skill-level date parsing so the two never disagree.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class SkillDateParsingDefaults
{
    public const int MinPlausibleCalendarYear = 1800;
    public const int MaxPlausibleCalendarYear = 2200;

    /// <summary>
    /// ISO 8601 shapes, tried first and always with InvariantCulture. A non-Gregorian culture such as
    /// th-TH would otherwise read "2026-03-04" as the Buddhist year 2026 (Gregorian 1483), and ar-SA
    /// (Umm al-Qura) would reject it outright.
    /// </summary>
    public static readonly string[] IsoDateTimeFormats =
    {
        "yyyy-MM-dd",
        "yyyy-MM-ddTHH:mmK",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
        "yyyy-MM-dd HH:mmK",
        "yyyy-MM-dd HH:mm:ss.FFFFFFFK"
    };

    public static readonly string[] IsoTimeFormats =
    {
        "HH:mm",
        "HH:mm:ss",
        "HH:mm:ss.FFFFFFF"
    };

    public static bool IsPlausibleCalendarYear(int year) =>
        year >= MinPlausibleCalendarYear && year <= MaxPlausibleCalendarYear;
}
