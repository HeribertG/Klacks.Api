// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Positive, per-date availability match of a client's hour-granular records against a shift's window on
/// a given date. Availability is the WEAKEST scheduling layer (Breaks and Keywords override it — the
/// context builders drop availability on those days before this result is consumed) and is opt-in PER
/// DATE. Semantics for the requested date:
/// <list type="bullet">
/// <item>No record for the date: no restriction — the day is open for work.</item>
/// <item>At least one <see cref="ClientAvailability.IsAvailable"/>=true record: the day is configured
/// positively, so only the explicitly available hours are usable. Any hour the shift occupies that is
/// not marked available makes the client unavailable.</item>
/// <item>Only IsAvailable=false records (no available hour): legacy negative semantics — only those
/// explicit unavailable hours block (preserves existing opt-out data).</item>
/// </list>
/// No data access — the caller loads the rows; this is a side-effect-free function. Cross-midnight shifts
/// are evaluated for the portion on the given date only (the next-day hours are a documented v1
/// limitation, since records are keyed by date + hour).
/// </summary>

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

        if (availableHours.Count == 0 && unavailableHours.Count == 0)
        {
            return false;
        }

        if (availableHours.Count > 0)
        {
            return OccupiedHours(start, end).Any(hour => !availableHours.Contains(hour));
        }

        return OccupiedHours(start, end).Any(unavailableHours.Contains);
    }

    private static IEnumerable<int> OccupiedHours(TimeOnly start, TimeOnly end)
    {
        var firstHour = start.Hour;
        int lastHour;

        if (end > start)
        {
            lastHour = end.Minute == 0 && end.Second == 0 ? end.Hour - 1 : end.Hour;
        }
        else
        {
            lastHour = 23;
        }

        for (var hour = firstHour; hour <= lastHour; hour++)
        {
            yield return hour;
        }
    }
}
