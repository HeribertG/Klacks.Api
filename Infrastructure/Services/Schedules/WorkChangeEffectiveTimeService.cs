// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
    private static readonly WorkChangeType[] AfterShiftTypes =
        [WorkChangeType.CorrectionEnd, WorkChangeType.TravelEnd, WorkChangeType.Debriefing];

    private static readonly WorkChangeType[] BeforeShiftTypes =
        [WorkChangeType.CorrectionStart, WorkChangeType.TravelStart, WorkChangeType.Briefing];

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
            WorkChangeType.ReplacementStart or WorkChangeType.ReplacementEnd
                => ReplacementWindow.Compute(
                    workChange.Type, work.StartTime, work.EndTime, workChange.StartTime, workChange.EndTime, workChange.ChangeTime),
            _ => (workChange.StartTime, workChange.EndTime),
        };
    }

    private async Task<(TimeOnly Start, TimeOnly End)> ComputeAfterShiftTimesAsync(
        WorkChange workChange, Work work)
    {
        var (beforeOffset, afterOffset) = await ComputeOffsetsAsync(
            workChange, work.Id, AfterShiftTypes, AfterShiftPriority);

        return (AddHours(work.EndTime, beforeOffset), AddHours(work.EndTime, afterOffset));
    }

    private async Task<(TimeOnly Start, TimeOnly End)> ComputeBeforeShiftTimesAsync(
        WorkChange workChange, Work work)
    {
        var (beforeOffset, afterOffset) = await ComputeOffsetsAsync(
            workChange, work.Id, BeforeShiftTypes, BeforeShiftPriority);

        return (SubtractHours(work.StartTime, afterOffset), SubtractHours(work.StartTime, beforeOffset));
    }

    /// <summary>
    /// Cumulative offset of <paramref name="workChange"/> among its siblings, ordered like the stored procedure
    /// (priority, then Id). The siblings are the persisted rows overlaid with the change tracker's unsaved state
    /// and the passed-in entry itself, so a new or edited entry is placed with its current ChangeTime; the rows are
    /// read without tracking so a detached entity with the same key can still be attached afterwards.
    /// </summary>
    private async Task<(decimal BeforeOffset, decimal AfterOffset)> ComputeOffsetsAsync(
        WorkChange workChange, Guid workId, WorkChangeType[] types, Func<WorkChangeType, int> priority)
    {
        var siblings = await LoadSiblingsAsync(workChange, workId, types);

        var ordered = siblings
            .OrderBy(wc => priority(wc.Type))
            .ThenBy(wc => wc.Id)
            .ToList();

        decimal beforeOffset = 0m, afterOffset = 0m;

        foreach (var entry in ordered)
        {
            if (ReferenceEquals(entry, workChange))
            {
                afterOffset = beforeOffset + entry.ChangeTime;
                break;
            }

            beforeOffset += entry.ChangeTime;
        }

        return (beforeOffset, afterOffset);
    }

    private async Task<List<WorkChange>> LoadSiblingsAsync(
        WorkChange workChange, Guid workId, WorkChangeType[] types)
    {
        var persisted = await _context.WorkChange
            .AsNoTracking()
            .Where(wc => wc.WorkId == workId && types.Contains(wc.Type))
            .ToListAsync();

        var siblings = persisted.ToDictionary(wc => wc.Id);

        var autoDetectChanges = _context.ChangeTracker.AutoDetectChangesEnabled;
        _context.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            foreach (var entry in _context.ChangeTracker.Entries<WorkChange>())
            {
                var tracked = entry.Entity;
                var isSibling = tracked.WorkId == workId && types.Contains(tracked.Type)
                    && entry.State != EntityState.Deleted && !tracked.IsDeleted;

                if (isSibling)
                {
                    siblings[tracked.Id] = tracked;
                }
                else
                {
                    siblings.Remove(tracked.Id);
                }
            }
        }
        finally
        {
            _context.ChangeTracker.AutoDetectChangesEnabled = autoDetectChanges;
        }

        siblings[workChange.Id] = workChange;

        return siblings.Values.ToList();
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

    private static TimeOnly AddHours(TimeOnly time, decimal hours) => ReplacementWindow.AddHours(time, hours);

    private static TimeOnly SubtractHours(TimeOnly time, decimal hours) => AddHours(time, -hours);
}
