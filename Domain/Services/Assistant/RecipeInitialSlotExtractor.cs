// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// W5.3: deterministic first-message slot extraction for recipe plans. Before the plan asks its
/// first question, the engine fills whatever it can read directly from the user's message — clock
/// times, weekdays (Montag…Sonntag, Mo…So) and dates — so the plan's "an ask whose slot is already
/// filled is skipped" rule reduces the real question count. The extraction is precision-biased: a
/// pattern fires only when it is unambiguous, otherwise the plan simply asks (same behaviour as
/// without this class). Group names are NOT extracted here because they must be validated against
/// the database (see FindMentionedGroupName and the engine wiring) — an invented group name must
/// never silently fill the slot.
///
/// Precision rules, each of them the fix for a measured false positive:
/// - A time needs a DIGIT ANCHOR plus an explicit clock marker: either colon minutes (14:30) or the
///   word "Uhr" (14 Uhr, 14.30 Uhr) or the space-free hour marker (14h, 14h30). A bare number is
///   never a time — "5 Mitarbeiter" used to yield 05:00, "PLZ 8001" 08:00 and "2026-09-01" 20:00.
///   A SPACED "h" is deliberately NOT accepted: in German "5 h" is a duration, not a clock time.
/// - A date must be a complete, CALENDAR-VALID date. Every candidate is normalised to yyyy-MM-dd and
///   run through DateTime.TryParseExact, so 31.02.2026 yields nothing instead of a phantom date.
/// - Two-letter weekday abbreviations match case-SENSITIVELY. Lower-cased "so" and "do" are ordinary
///   German/English filler words and used to fill the weekday slot from a sentence about nothing.
/// </summary>

using System.Globalization;
using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant;

public static class RecipeInitialSlotExtractor
{
    private const string IsoDateFormat = "yyyy-MM-dd";
    private const int TwoDigitYearBase = 2000;

    /// <summary>
    /// Clock times, longest alternative first so "14:30" is not consumed as the bare hour "14".
    /// Every alternative carries an explicit marker; none of them matches a plain number.
    /// </summary>
    private static readonly Regex TimePattern = new(
        @"(?<!\d)(?:"
        + @"(?<hour>[01]?\d|2[0-3])[.:](?<minute>[0-5]\d)\s*Uhr\b"
        + @"|(?<hour>[01]?\d|2[0-3]):(?<minute>[0-5]\d)(?!\d)"
        + @"|(?<hour>[01]?\d|2[0-3])h(?<minute>[0-5]\d)(?!\d)"
        + @"|(?<hour>[01]?\d|2[0-3])\s*Uhr\b"
        + @"|(?<hour>[01]?\d|2[0-3])h\b"
        + @")",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex IsoDatePattern = new(
        @"(?<!\d)(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})(?!\d)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// German numeric date. The year is optional (1.9. is an everyday, unambiguous German date) but
    /// the SECOND dot is not, which keeps decimal numbers and version strings out.
    /// </summary>
    private static readonly Regex CompactDatePattern = new(
        @"(?<!\d)(?<day>\d{1,2})\.(?<month>\d{1,2})\.(?:(?<year>\d{4}|\d{2})(?!\d))?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex MonthNameDatePattern = new(
        @"(?<!\d)(?<day>\d{1,2})\.?\s+(?<month>Januar|Februar|März|Maerz|April|Mai|Juni|Juli|August|September|Oktober|November|Dezember)\s+(?<year>\d{4})(?!\d)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly string[] GermanMonthNames =
    [
        "januar", "februar", "märz", "april", "mai", "juni",
        "juli", "august", "september", "oktober", "november", "dezember"
    ];

    private const string AlternateMarchName = "maerz";
    private const int MarchOrdinal = 3;

    /// <summary>
    /// Full names match case-insensitively; the two-letter abbreviation only in its written form, so
    /// the German filler words "so" and "do" cannot fill a weekday slot.
    /// </summary>
    private static readonly (string FullName, string Abbreviation, string Canonical)[] WeekdayGroups =
    [
        ("Montag", "Mo", "Montag"),
        ("Dienstag", "Di", "Dienstag"),
        ("Mittwoch", "Mi", "Mittwoch"),
        ("Donnerstag", "Do", "Donnerstag"),
        ("Freitag", "Fr", "Freitag"),
        ("Samstag", "Sa", "Samstag"),
        ("Sonntag", "So", "Sonntag"),
    ];

    public static Dictionary<string, string> Extract(
        string? message, IReadOnlyCollection<string> slotNames)
    {
        var slots = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(message) || slotNames.Count == 0)
        {
            return slots;
        }

        var slotSet = new HashSet<string>(slotNames, StringComparer.OrdinalIgnoreCase);

        TryFillTimes(message, slotSet, slots);
        TryFillDates(message, slotSet, slots);
        TryFillWeekdays(message, slotSet, slots);

        return slots;
    }

    /// <summary>
    /// Returns the longest group name from the candidate list that appears in the message as a
    /// word-boundary match (accent-insensitive), or null when none does. Longest-first prevents
    /// "Deutschschweiz" from being beaten by a partial "Deutsch" when both exist.
    /// </summary>
    public static string? FindMentionedGroupName(string? message, IReadOnlyCollection<string> groupNames)
    {
        if (string.IsNullOrWhiteSpace(message) || groupNames.Count == 0)
        {
            return null;
        }

        return groupNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderByDescending(name => name.Length)
            .FirstOrDefault(name => Regex.IsMatch(
                message,
                $@"\b{Regex.Escape(name)}\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
    }

    private static void TryFillTimes(
        string message, HashSet<string> slotSet, Dictionary<string, string> slots)
    {
        var matches = TimePattern.Matches(message)
            .Select(NormalizeTime)
            .Where(time => time != null)
            .Select(time => time!)
            .ToList();

        if (matches.Count == 0)
        {
            return;
        }

        FillPair(slotSet, slots, "startTime", "endTime", matches);
        FillPair(slotSet, slots, "fromTime", "untilTime", matches);
    }

    private static void TryFillDates(
        string message, HashSet<string> slotSet, Dictionary<string, string> slots)
    {
        var dates = new List<string>();

        foreach (Match match in IsoDatePattern.Matches(message))
        {
            AddDate(dates, match.Groups["year"].Value, match.Groups["month"].Value, match.Groups["day"].Value);
        }

        foreach (Match match in CompactDatePattern.Matches(message))
        {
            AddDate(dates, ResolveYear(match.Groups["year"]), match.Groups["month"].Value, match.Groups["day"].Value);
        }

        foreach (Match match in MonthNameDatePattern.Matches(message))
        {
            var month = ResolveMonthName(match.Groups["month"].Value);
            if (month > 0)
            {
                AddDate(dates, match.Groups["year"].Value, month.ToString(CultureInfo.InvariantCulture), match.Groups["day"].Value);
            }
        }

        if (dates.Count == 0)
        {
            return;
        }

        FillPair(slotSet, slots, "startDate", "untilDate", dates);
        FillPair(slotSet, slots, "fromDate", "untilDate", dates);
    }

    /// <summary>
    /// Fills a from/to slot pair from an ordered list of readings: the first reading goes into the
    /// opening slot, the second (or the first again when only one was found) into the closing one.
    /// </summary>
    private static void FillPair(
        HashSet<string> slotSet,
        Dictionary<string, string> slots,
        string openingSlot,
        string closingSlot,
        IReadOnlyList<string> readings)
    {
        if (slotSet.Contains(openingSlot) && !slots.ContainsKey(openingSlot))
        {
            slots[openingSlot] = readings[0];
        }

        if (slotSet.Contains(closingSlot) && !slots.ContainsKey(closingSlot))
        {
            slots[closingSlot] = readings.Count > 1 ? readings[1] : readings[0];
        }
    }

    private static void TryFillWeekdays(
        string message, HashSet<string> slotSet, Dictionary<string, string> slots)
    {
        if (!slotSet.Contains("weekdays") && !slotSet.Contains("weekday"))
        {
            return;
        }

        var found = new List<string>();
        foreach (var (fullName, abbreviation, canonical) in WeekdayGroups)
        {
            var matched =
                Regex.IsMatch(message, $@"\b{Regex.Escape(fullName)}\b",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                || Regex.IsMatch(message, $@"\b{Regex.Escape(abbreviation)}\b",
                    RegexOptions.CultureInvariant);

            if (matched)
            {
                found.Add(canonical);
            }
        }

        if (found.Count == 0)
        {
            return;
        }

        var value = string.Join(", ", found);
        if (slotSet.Contains("weekdays"))
        {
            slots["weekdays"] = value;
        }

        if (slotSet.Contains("weekday"))
        {
            slots["weekday"] = found[0];
        }
    }

    /// <summary>
    /// Normalises the components to yyyy-MM-dd and keeps the value only when it is a real calendar
    /// date — TryParseExact is what rejects 31.02. instead of inventing a day that does not exist.
    /// </summary>
    private static void AddDate(List<string> dates, string year, string month, string day)
    {
        var candidate = $"{year.PadLeft(4, '0')}-{month.PadLeft(2, '0')}-{day.PadLeft(2, '0')}";
        if (DateTime.TryParseExact(candidate, IsoDateFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out _))
        {
            dates.Add(candidate);
        }
    }

    private static string ResolveYear(Group yearGroup)
    {
        if (!yearGroup.Success)
        {
            return DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
        }

        return yearGroup.Value.Length == 2
            ? (TwoDigitYearBase + int.Parse(yearGroup.Value, CultureInfo.InvariantCulture))
                .ToString(CultureInfo.InvariantCulture)
            : yearGroup.Value;
    }

    private static int ResolveMonthName(string name)
    {
        var normalized = name.ToLowerInvariant();
        if (string.Equals(normalized, AlternateMarchName, StringComparison.Ordinal))
        {
            return MarchOrdinal;
        }

        return Array.IndexOf(GermanMonthNames, normalized) + 1;
    }

    private static string? NormalizeTime(Match match)
    {
        if (!match.Success)
        {
            return null;
        }

        var hour = int.Parse(match.Groups["hour"].Value, CultureInfo.InvariantCulture);
        var minute = match.Groups["minute"].Success
            ? int.Parse(match.Groups["minute"].Value, CultureInfo.InvariantCulture)
            : 0;

        return $"{hour:00}:{minute:00}";
    }
}
