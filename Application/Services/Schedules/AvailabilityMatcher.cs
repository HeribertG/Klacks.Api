// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure, time-aware match of a client's hour-granular availability against a shift's time window on a
/// given date. Availability is opt-in and sparse: a missing record means "no restriction" (available),
/// mirroring how a missing Break means "not absent". Only an explicit unavailable record
/// (<see cref="ClientAvailability.IsAvailable"/> = false) that overlaps an hour the shift occupies
/// blocks the client. No data access — the caller loads the rows; this is a side-effect-free function.
/// Cross-midnight shifts are evaluated for the portion on the given date only (the next-day hours are a
/// documented v1 limitation, since availability rows are keyed by date + hour).
/// </summary>

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Services.Schedules;

public static class AvailabilityMatcher
{
    public static bool IsExplicitlyUnavailable(
        IReadOnlyList<ClientAvailability> entries,
        TimeOnly start,
        TimeOnly end)
    {
        if (entries.Count == 0)
        {
            return false;
        }

        var unavailableHours = entries
            .Where(e => !e.IsAvailable)
            .Select(e => e.Hour)
            .ToHashSet();
        if (unavailableHours.Count == 0)
        {
            return false;
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
