// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Pure, deterministic mapper that projects the works of a previous period onto a new period, week for
/// week and weekday-true (Monday to Monday). Both periods are anchored to the Monday of the week that
/// contains their first day, so a Wednesday source work always lands on a Wednesday target date. Target
/// days that overhang the source (new period longer than the previous one) repeat the last full source
/// week; extra source weeks (previous period longer) are dropped by never being referenced.
/// </summary>
/// <param name="sourceWorks">Works of the previous period, one entry per real Work</param>
/// <param name="previousPeriodFrom">Previous period start (inclusive)</param>
/// <param name="previousPeriodUntil">Previous period end (inclusive)</param>
/// <param name="targetPeriodFrom">Target period start (inclusive)</param>
/// <param name="targetPeriodUntil">Target period end (inclusive)</param>
public static class WarmStartWeekdayMapper
{
    private const int DaysPerWeek = 7;

    public static IReadOnlyList<CoreWarmStartAssignment> Map(
        IReadOnlyList<WarmStartSourceWork> sourceWorks,
        DateOnly previousPeriodFrom,
        DateOnly previousPeriodUntil,
        DateOnly targetPeriodFrom,
        DateOnly targetPeriodUntil)
    {
        if (sourceWorks.Count == 0 || targetPeriodUntil < targetPeriodFrom)
        {
            return [];
        }

        var previousAnchor = MondayOf(previousPeriodFrom);
        var targetAnchor = MondayOf(targetPeriodFrom);

        var worksByDate = new Dictionary<DateOnly, List<WarmStartSourceWork>>();
        foreach (var work in sourceWorks)
        {
            if (!worksByDate.TryGetValue(work.Date, out var list))
            {
                list = [];
                worksByDate[work.Date] = list;
            }

            list.Add(work);
        }

        var lastFullWeekIndex = ResolveLastFullWeekIndex(
            worksByDate.Keys, previousAnchor, previousPeriodFrom, previousPeriodUntil);

        var assignments = new List<CoreWarmStartAssignment>();
        for (var target = targetPeriodFrom; target <= targetPeriodUntil; target = target.AddDays(1))
        {
            var targetOffset = target.DayNumber - targetAnchor.DayNumber;
            var targetWeekIndex = targetOffset / DaysPerWeek;
            var dayOffset = targetOffset % DaysPerWeek;

            var sourceWeekIndex = targetWeekIndex <= lastFullWeekIndex ? targetWeekIndex : lastFullWeekIndex;
            var sourceDate = previousAnchor.AddDays(sourceWeekIndex * DaysPerWeek + dayOffset);

            if (!worksByDate.TryGetValue(sourceDate, out var dayWorks))
            {
                continue;
            }

            foreach (var work in dayWorks)
            {
                var startAt = target.ToDateTime(work.StartTime);
                var endAt = work.EndTime <= work.StartTime
                    ? target.AddDays(1).ToDateTime(work.EndTime)
                    : target.ToDateTime(work.EndTime);

                assignments.Add(new CoreWarmStartAssignment(
                    AgentId: work.AgentId.ToString(),
                    Date: target,
                    ShiftRefId: work.ShiftId,
                    StartAt: startAt,
                    EndAt: endAt,
                    TotalHours: work.WorkTime));
            }
        }

        return assignments;
    }

    private static int ResolveLastFullWeekIndex(
        IEnumerable<DateOnly> sourceDates,
        DateOnly previousAnchor,
        DateOnly previousPeriodFrom,
        DateOnly previousPeriodUntil)
    {
        var lastFullWeekIndex = -1;
        var maxWeekIndexWithData = 0;

        foreach (var date in sourceDates)
        {
            var weekIndex = (date.DayNumber - previousAnchor.DayNumber) / DaysPerWeek;
            if (weekIndex > maxWeekIndexWithData)
            {
                maxWeekIndexWithData = weekIndex;
            }

            var weekStart = previousAnchor.AddDays(weekIndex * DaysPerWeek);
            var weekEnd = weekStart.AddDays(DaysPerWeek - 1);
            if (weekStart >= previousPeriodFrom && weekEnd <= previousPeriodUntil && weekIndex > lastFullWeekIndex)
            {
                lastFullWeekIndex = weekIndex;
            }
        }

        return lastFullWeekIndex >= 0 ? lastFullWeekIndex : maxWeekIndexWithData;
    }

    private static DateOnly MondayOf(DateOnly date)
    {
        var mondayOffset = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-mondayOffset);
    }
}
