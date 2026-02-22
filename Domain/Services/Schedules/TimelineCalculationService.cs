// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public class TimelineCalculationService : ITimelineCalculationService
{
    public List<TimeRect> CalculateTimeRects(List<Work> works, List<WorkChange> workChanges, List<Break> breaks)
    {
        var result = new List<TimeRect>();
        var changesByWorkId = workChanges.GroupBy(wc => wc.WorkId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var work in works)
        {
            var effectiveStart = work.StartTime;
            var effectiveEnd = work.EndTime;

            if (changesByWorkId.TryGetValue(work.Id, out var changes))
            {
                foreach (var change in changes)
                {
                    switch (change.Type)
                    {
                        case WorkChangeType.CorrectionStart:
                            effectiveStart = change.StartTime;
                            break;
                        case WorkChangeType.CorrectionEnd:
                            effectiveEnd = change.EndTime;
                            break;
                        case WorkChangeType.ReplacementStart:
                            effectiveStart = change.EndTime;
                            break;
                        case WorkChangeType.ReplacementEnd:
                            effectiveEnd = change.StartTime;
                            break;
                    }
                }

                foreach (var change in changes.Where(c =>
                    c.Type is WorkChangeType.CorrectionStart or WorkChangeType.CorrectionEnd))
                {
                    AddRectsWithMidnightSplit(result, work.Id, TimeRectSourceType.Correction,
                        work.ClientId, work.CurrentDate, change.StartTime, change.EndTime);
                }

                foreach (var change in changes.Where(c =>
                    c.Type is (WorkChangeType.ReplacementStart or WorkChangeType.ReplacementEnd) &&
                    c.ReplaceClientId.HasValue))
                {
                    AddRectsWithMidnightSplit(result, work.Id, TimeRectSourceType.Replacement,
                        change.ReplaceClientId!.Value, work.CurrentDate, change.StartTime, change.EndTime);
                }
            }

            AddRectsWithMidnightSplit(result, work.Id, TimeRectSourceType.Work,
                work.ClientId, work.CurrentDate, effectiveStart, effectiveEnd);
        }

        foreach (var b in breaks)
        {
            AddRectsWithMidnightSplit(result, b.Id, TimeRectSourceType.Break,
                b.ClientId, b.CurrentDate, b.StartTime, b.EndTime);
        }

        return result;
    }

    private static void AddRectsWithMidnightSplit(
        List<TimeRect> result,
        Guid sourceId,
        TimeRectSourceType sourceType,
        Guid clientId,
        DateOnly date,
        TimeOnly start,
        TimeOnly end)
    {
        if (end < start)
        {
            result.Add(new TimeRect(sourceId, sourceType, clientId, date, start, TimeOnly.MaxValue));
            result.Add(new TimeRect(sourceId, sourceType, clientId, date.AddDays(1), TimeOnly.MinValue, end));
        }
        else
        {
            result.Add(new TimeRect(sourceId, sourceType, clientId, date, start, end));
        }
    }
}
