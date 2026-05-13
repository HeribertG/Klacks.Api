// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Computes the effective Von/Bis window for a WorkChange entry by replicating
/// the cumulative offset logic of the get_schedule_entries stored procedure.
/// </summary>
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public class WorkChangeEffectiveTimeService : IWorkChangeEffectiveTimeService
{
    private readonly DataBaseContext _context;

    public WorkChangeEffectiveTimeService(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<(TimeOnly Start, TimeOnly End)> GetEffectiveTimesAsync(
        WorkChange workChange, Work work, Shift? shift)
    {
        return workChange.Type switch
        {
            WorkChangeType.CorrectionEnd or WorkChangeType.TravelEnd or WorkChangeType.Debriefing
                => await ComputeAfterShiftTimesAsync(workChange, work),
            WorkChangeType.CorrectionStart or WorkChangeType.TravelStart or WorkChangeType.Briefing
                => await ComputeBeforeShiftTimesAsync(workChange, work),
            WorkChangeType.ReplacementStart
                => (work.StartTime, AddHours(work.StartTime, workChange.ChangeTime)),
            WorkChangeType.ReplacementEnd
                => (SubtractHours(work.EndTime, workChange.ChangeTime), work.EndTime),
            _ => (workChange.StartTime, workChange.EndTime),
        };
    }

    private async Task<(TimeOnly Start, TimeOnly End)> ComputeAfterShiftTimesAsync(
        WorkChange workChange, Work work)
    {
        var siblings = await _context.WorkChange
            .Where(wc => wc.WorkId == work.Id
                && (wc.Type == WorkChangeType.CorrectionEnd
                    || wc.Type == WorkChangeType.TravelEnd
                    || wc.Type == WorkChangeType.Debriefing))
            .ToListAsync();

        var ordered = siblings
            .OrderBy(wc => AfterShiftPriority(wc.Type))
            .ThenBy(wc => wc.Id)
            .ToList();

        decimal beforeOffset = 0m, afterOffset = 0m;

        foreach (var entry in ordered)
        {
            if (entry.Id == workChange.Id)
            {
                afterOffset = beforeOffset + entry.ChangeTime;
                break;
            }

            beforeOffset += entry.ChangeTime;
        }

        return (AddHours(work.EndTime, beforeOffset), AddHours(work.EndTime, afterOffset));
    }

    private async Task<(TimeOnly Start, TimeOnly End)> ComputeBeforeShiftTimesAsync(
        WorkChange workChange, Work work)
    {
        var siblings = await _context.WorkChange
            .Where(wc => wc.WorkId == work.Id
                && (wc.Type == WorkChangeType.CorrectionStart
                    || wc.Type == WorkChangeType.TravelStart
                    || wc.Type == WorkChangeType.Briefing))
            .ToListAsync();

        var ordered = siblings
            .OrderBy(wc => BeforeShiftPriority(wc.Type))
            .ThenBy(wc => wc.Id)
            .ToList();

        decimal beforeOffset = 0m, afterOffset = 0m;

        foreach (var entry in ordered)
        {
            if (entry.Id == workChange.Id)
            {
                afterOffset = beforeOffset + entry.ChangeTime;
                break;
            }

            beforeOffset += entry.ChangeTime;
        }

        return (SubtractHours(work.StartTime, afterOffset), SubtractHours(work.StartTime, beforeOffset));
    }

    private static int AfterShiftPriority(WorkChangeType type) => type switch
    {
        WorkChangeType.CorrectionEnd => 0,
        WorkChangeType.Debriefing => 1,
        WorkChangeType.TravelEnd => 2,
        _ => int.MaxValue,
    };

    private static int BeforeShiftPriority(WorkChangeType type) => type switch
    {
        WorkChangeType.CorrectionStart => 0,
        WorkChangeType.Briefing => 1,
        WorkChangeType.TravelStart => 2,
        _ => int.MaxValue,
    };

    private static TimeOnly AddHours(TimeOnly time, decimal hours)
    {
        var totalMs = (long)decimal.Round(hours * 3_600_000m);
        var span = time.ToTimeSpan().Add(TimeSpan.FromMilliseconds(totalMs));
        var normalized = TimeSpan.FromTicks(
            ((span.Ticks % TimeSpan.TicksPerDay) + TimeSpan.TicksPerDay) % TimeSpan.TicksPerDay);
        return TimeOnly.FromTimeSpan(normalized);
    }

    private static TimeOnly SubtractHours(TimeOnly time, decimal hours) =>
        AddHours(time, -hours);
}
