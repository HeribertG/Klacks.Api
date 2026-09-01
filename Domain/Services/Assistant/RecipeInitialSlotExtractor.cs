// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// W5.3: deterministic first-message slot extraction for recipe plans. Before the plan asks its
/// first question, the engine fills whatever it can read directly from the user's message — start
/// and end times (HH:mm), weekdays (Montag…Sonntag, Mo…So), ISO dates (yyyy-MM-dd) and compact
/// German dates (1.9. / 01.09.) — so the plan's "ask whose slot is already filled is skipped" rule
/// reduces the real question count. The extraction is precision-biased: a pattern fires only when it
/// is unambiguous, otherwise the plan simply asks (same behaviour as today). Group names are NOT
/// extracted here because they must be validated against the database (see FindMentionedGroupName
/// and the engine wiring) — an invented group name must never silently fill the slot.
/// </summary>

using System.Globalization;
using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant;

public static class RecipeInitialSlotExtractor
{
    private static readonly Regex TimePattern = new(
        @"(?<!\d)([01]?\d|2[0-3])(?:[:.]([0-5]\d))?\s*(?:Uhr)?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex IsoDatePattern = new(
        @"\b(?<year>20\d{2})-(?<month>0[1-9]|1[0-2])-(?<day>0[1-9]|[12]\d|3[01])\b",
        RegexOptions.Compiled);

    private static readonly Regex CompactDatePattern = new(
        @"(?<!\d)(?<day>0?[1-9]|[12]\d|3[01])\.(?<month>0?[1-9]|1[0-2])\.(?:((?:20)?(?<year>\d{2}|\d{4})))?(?!\w)",
        RegexOptions.Compiled);

    private static readonly (string[] Names, string Canonical)[] WeekdayGroups =
    [
        (["Montag", "Mo"], "Montag"),
        (["Dienstag", "Di"], "Dienstag"),
        (["Mittwoch", "Mi"], "Mittwoch"),
        (["Donnerstag", "Do"], "Donnerstag"),
        (["Freitag", "Fr"], "Freitag"),
        (["Samstag", "Sa"], "Samstag"),
        (["Sonntag", "So"], "Sonntag"),
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
            .Select(m => NormalizeTime(m.Value))
            .Where(time => time != null)
            .Select(time => time!)
            .ToList();

        if (matches.Count == 0)
        {
            return;
        }

        if (slotSet.Contains("startTime") && !slots.ContainsKey("startTime"))
        {
            slots["startTime"] = matches[0];
        }

        if (slotSet.Contains("fromTime") && !slots.ContainsKey("fromTime"))
        {
            slots["fromTime"] = matches[0];
        }

        var second = matches.Count > 1 ? matches[1] : matches[0];
        if (slotSet.Contains("endTime") && !slots.ContainsKey("endTime"))
        {
            slots["endTime"] = second;
        }

        if (slotSet.Contains("untilTime") && !slots.ContainsKey("untilTime"))
        {
            slots["untilTime"] = second;
        }
    }

    private static void TryFillDates(
        string message, HashSet<string> slotSet, Dictionary<string, string> slots)
    {
        var dates = new List<string>();

        foreach (Match match in IsoDatePattern.Matches(message))
        {
            dates.Add($"{match.Groups["year"].Value}-{match.Groups["month"].Value}-{match.Groups["day"].Value}");
        }

        foreach (Match match in CompactDatePattern.Matches(message))
        {
            var year = match.Groups["year"].Success
                ? (match.Groups["year"].Value.Length == 2
                    ? $"20{match.Groups["year"].Value}"
                    : match.Groups["year"].Value)
                : DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
            var month = match.Groups["month"].Value.PadLeft(2, '0');
            var day = match.Groups["day"].Value.PadLeft(2, '0');
            dates.Add($"{year}-{month}-{day}");
        }

        if (dates.Count == 0)
        {
            return;
        }

        if (slotSet.Contains("startDate") && !slots.ContainsKey("startDate"))
        {
            slots["startDate"] = dates[0];
        }

        if (slotSet.Contains("fromDate") && !slots.ContainsKey("fromDate"))
        {
            slots["fromDate"] = dates[0];
        }

        var second = dates.Count > 1 ? dates[1] : dates[0];
        if (slotSet.Contains("untilDate") && !slots.ContainsKey("untilDate"))
        {
            slots["untilDate"] = second;
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
        foreach (var (names, canonical) in WeekdayGroups)
        {
            if (names.Any(name => Regex.IsMatch(
                    message,
                    $@"\b{Regex.Escape(name)}\b",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)))
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

    private static string? NormalizeTime(string raw)
    {
        var match = TimePattern.Match(raw);
        if (!match.Success)
        {
            return null;
        }

        var hour = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var minute = match.Groups[2].Success
            ? int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)
            : 0;

        if (hour is < 0 or > 23 || minute is < 0 or > 59)
        {
            return null;
        }

        return $"{hour:00}:{minute:00}";
    }
}
