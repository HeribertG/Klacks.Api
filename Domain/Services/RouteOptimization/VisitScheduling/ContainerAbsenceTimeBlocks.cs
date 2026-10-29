// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Turns the absence (break) items of a container template into what route optimization and visit scheduling read,
/// with the same rules as the Ui container template page: fixed intervals only from breaks with both a start and an
/// end (ending one day later when the end is not after the start, so equal bounds become a full day), and time blocks
/// for every break - fixed with its own times, movable with the duration of its time range (00:00-00:30 when unset),
/// wrapped past midnight and at least one minute long.
/// </summary>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.RouteOptimization.VisitScheduling;

public static class ContainerAbsenceTimeBlocks
{
    public static List<FixedTimeInterval> BuildFixedIntervals(IEnumerable<ContainerAbsenceItem> absences)
    {
        return absences
            .Where(a => a.StartItem.HasValue && a.EndItem.HasValue)
            .Select(a =>
            {
                var start = ContainerVisitScheduler.ClockMinutes(a.StartItem!.Value);
                var end = ContainerVisitScheduler.ClockMinutes(a.EndItem!.Value);
                return new FixedTimeInterval(start, end > start ? end : end + ContainerVisitDefaults.MinutesPerDay);
            })
            .OrderBy(interval => interval.StartMinutes)
            .ToList();
    }

    public static List<TimeBlock> BuildTimeBlocks(IEnumerable<ContainerAbsenceItem> absences)
    {
        return absences.Select(ToTimeBlock).ToList();
    }

    private static TimeBlock ToTimeBlock(ContainerAbsenceItem absence)
    {
        var hasFixedTime = absence.StartItem.HasValue && absence.EndItem.HasValue;
        var durationMinutes = hasFixedTime
            ? ContainerVisitScheduler.ClockMinutes(absence.EndItem!.Value) - ContainerVisitScheduler.ClockMinutes(absence.StartItem!.Value)
            : MinutesOrDefault(absence.TimeRangeEndItem, ContainerVisitDefaults.MovableBlockDefaultEndMinutes)
                - MinutesOrDefault(absence.TimeRangeStartItem, ContainerVisitDefaults.MovableBlockDefaultStartMinutes);

        if (durationMinutes < 0)
        {
            durationMinutes += ContainerVisitDefaults.MinutesPerDay;
        }

        return new TimeBlock(
            absence.AbsenceId,
            string.Empty,
            hasFixedTime ? absence.StartItem : null,
            hasFixedTime ? absence.EndItem : null,
            TimeSpan.FromMinutes(Math.Max(durationMinutes, ContainerVisitDefaults.MinBlockDurationMinutes)),
            !hasFixedTime);
    }

    private static int MinutesOrDefault(TimeOnly? time, int defaultMinutes) =>
        time.HasValue ? ContainerVisitScheduler.ClockMinutes(time.Value) : defaultMinutes;
}
