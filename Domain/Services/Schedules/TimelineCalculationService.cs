// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Calculates ScheduleBlocks from Work, WorkChange and Break entries.
/// Uses absolute DateTime intervals - no midnight splitting required.
/// </summary>
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public class TimelineCalculationService : ITimelineCalculationService
{
    public List<ScheduleBlock> CalculateScheduleBlocks(List<Work> works, List<WorkChange> workChanges, List<Break> breaks)
    {
        var result = new List<ScheduleBlock>();
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
                    result.Add(CreateBlock(work.Id, ScheduleBlockType.Correction,
                        work.ClientId, work.CurrentDate, change.StartTime, change.EndTime));
                }

                foreach (var change in changes.Where(c =>
                    c.Type is (WorkChangeType.ReplacementStart or WorkChangeType.ReplacementEnd) &&
                    c.ReplaceClientId.HasValue))
                {
                    result.Add(CreateBlock(work.Id, ScheduleBlockType.Replacement,
                        change.ReplaceClientId!.Value, work.CurrentDate, change.StartTime, change.EndTime));
                }
            }

            result.Add(CreateBlock(work.Id, ScheduleBlockType.Work,
                work.ClientId, work.CurrentDate, effectiveStart, effectiveEnd, work.ShiftId));
        }

        foreach (var b in breaks)
        {
            result.Add(CreateBlock(b.Id, ScheduleBlockType.Break,
                b.ClientId, b.CurrentDate, b.StartTime, b.EndTime));
        }

        return result;
    }

    private static ScheduleBlock CreateBlock(
        Guid sourceId,
        ScheduleBlockType blockType,
        Guid clientId,
        DateOnly date,
        TimeOnly start,
        TimeOnly end,
        Guid? shiftId = null)
    {
        var startDateTime = date.ToDateTime(start);
        var endDateTime = end <= start
            ? date.AddDays(1).ToDateTime(end)
            : date.ToDateTime(end);

        return new ScheduleBlock(sourceId, blockType, clientId, startDateTime, endDateTime, shiftId);
    }
}
