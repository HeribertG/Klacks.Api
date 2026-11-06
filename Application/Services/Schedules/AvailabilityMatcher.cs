// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Positive, per-date availability match of a client's hour-granular records against a shift's window on
/// a given date. A Break overrides it (the context builders drop availability on break days before this result is
/// consumed); a schedule keyword does not - availability stays hard on keyword days (owner decision 2026-11-05: a
/// keyword is a wish). Availability is opt-in PER DATE. Semantics for the requested date:
/// <list type="bullet">
/// <item>No record for the date: no restriction — the day is open for work.</item>
/// <item>At least one <see cref="ClientAvailability.IsAvailable"/>=true record: the day is configured
/// positively, so only the explicitly available hours are usable. Any hour the shift occupies that is
/// not marked available makes the client unavailable.</item>
/// <item>Only IsAvailable=false records (no available hour): legacy negative semantics — only those
/// explicit unavailable hours block (preserves existing opt-out data).</item>
/// </list>
/// No data access — the caller loads the rows; this is a side-effect-free function. A shift that crosses midnight is
/// checked on both dates: the hours up to midnight against the start date, the hours after midnight against the next
/// date, each with that date's own semantics (availability is hard, owner rule 2026-11-06). The caller therefore loads
/// the rows of the day after the last shift date as well.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Services.Schedules;

public static class AvailabilityMatcher
{
    public static bool IsUnavailable(
        IReadOnlyList<ClientAvailability> entries,
        DateOnly date,
        TimeOnly start,
        TimeOnly end)
    {
        if (entries.Count == 0)
        {
            return false;
        }

        if (IsUnavailableOnDate(entries, date, SameDayHours(start, end)))
        {
            return true;
        }

        return end <= start && IsUnavailableOnDate(entries, date.AddDays(1), NextDayHours(end));
    }

    private static bool IsUnavailableOnDate(IReadOnlyList<ClientAvailability> entries, DateOnly date, IReadOnlyList<int> occupiedHours)
    {
        if (occupiedHours.Count == 0)
        {
            return false;
        }

        var availableHours = new HashSet<int>();
        var unavailableHours = new HashSet<int>();
        foreach (var entry in entries)
        {
            if (entry.Date != date)
            {
                continue;
            }
            if (entry.IsAvailable)
            {
                availableHours.Add(entry.Hour);
            }
            else
            {
                unavailableHours.Add(entry.Hour);
            }
        }

        if (availableHours.Count > 0)
        {
            return occupiedHours.Any(hour => !availableHours.Contains(hour));
        }

        return occupiedHours.Any(unavailableHours.Contains);
    }

    private static IReadOnlyList<int> SameDayHours(TimeOnly start, TimeOnly end)
    {
        var lastHour = end > start ? LastOccupiedHour(end) : DayTimeConstants.LastHourOfDay;
        return HourRange(start.Hour, lastHour);
    }

    private static IReadOnlyList<int> NextDayHours(TimeOnly end)
        => HourRange(0, LastOccupiedHour(end));

    private static int LastOccupiedHour(TimeOnly end)
        => end.Minute == 0 && end.Second == 0 ? end.Hour - 1 : end.Hour;

    private static IReadOnlyList<int> HourRange(int firstHour, int lastHour)
        => lastHour < firstHour ? [] : Enumerable.Range(firstHour, lastHour - firstHour + 1).ToList();
}
