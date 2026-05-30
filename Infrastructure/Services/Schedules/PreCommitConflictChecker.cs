// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IPreCommitConflictChecker"/>. For each affected client it loads the existing
/// works/changes/breaks in a +/- BoundaryDays window, builds the schedule timeline once with and
/// once without synthetic blocks for the planned rows, runs the shared validation builders over both
/// and returns the violations present only in the augmented run (the ones the placement introduces).
/// Grouped per client so the timeline is loaded once per client even for batch placements.
/// </summary>
/// <param name="context">Read-only access to Work/WorkChange/Break for the affected clients</param>
/// <param name="timelineCalculator">Shared Work-to-ScheduleBlock mapper (same one the live validator uses)</param>
/// <param name="policyResolver">Resolves per-client rest/overtime/consecutive/weekly/min-rest thresholds</param>
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public sealed class PreCommitConflictChecker : IPreCommitConflictChecker
{
    private readonly DataBaseContext _context;
    private readonly ITimelineCalculationService _timelineCalculator;
    private readonly ISchedulingPolicyResolver _policyResolver;

    public PreCommitConflictChecker(
        DataBaseContext context,
        ITimelineCalculationService timelineCalculator,
        ISchedulingPolicyResolver policyResolver)
    {
        _context = context;
        _timelineCalculator = timelineCalculator;
        _policyResolver = policyResolver;
    }

    public async Task<PreCommitCheckResult> CheckAsync(
        IReadOnlyList<PlannedWorkRow> plannedRows,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default)
    {
        if (plannedRows.Count == 0)
        {
            return PreCommitCheckResult.Empty;
        }

        var windowStart = plannedRows.Min(r => r.Date).AddDays(-ScenarioConstants.BoundaryDays);
        var windowEnd = plannedRows.Max(r => r.Date).AddDays(ScenarioConstants.BoundaryDays);

        var newConflicts = new List<ScheduleValidationNotificationDto>();

        foreach (var group in plannedRows.GroupBy(r => r.ClientId))
        {
            var clientId = group.Key;

            var works = await LoadWorksAsync(clientId, windowStart, windowEnd, analyseToken, cancellationToken);
            var workIds = works.Select(w => w.Id).ToList();
            var workChanges = await LoadWorkChangesAsync(workIds, cancellationToken);
            var breaks = await LoadBreaksAsync(clientId, windowStart, windowEnd, analyseToken, cancellationToken);

            var policy = await _policyResolver.GetForClientAsync(clientId, group.Min(r => r.Date));

            var baselineBlocks = _timelineCalculator.CalculateScheduleBlocks(works, workChanges, breaks);

            var augmentedWorks = works.Concat(group.Select(ToSyntheticWork)).ToList();
            var augmentedBlocks = _timelineCalculator.CalculateScheduleBlocks(augmentedWorks, workChanges, breaks);

            var baselineEntries = Validate(baselineBlocks, clientId, windowStart, windowEnd, policy);
            var augmentedEntries = Validate(augmentedBlocks, clientId, windowStart, windowEnd, policy);

            var baselineKeys = baselineEntries.Select(BuildKey).ToHashSet();
            newConflicts.AddRange(augmentedEntries.Where(e => !baselineKeys.Contains(BuildKey(e))));
        }

        return new PreCommitCheckResult(newConflicts);
    }

    private static List<ScheduleValidationNotificationDto> Validate(
        List<ScheduleBlock> blocks,
        Guid clientId,
        DateOnly from,
        DateOnly to,
        SchedulingPolicy policy)
    {
        var timeline = new ClientTimeline(clientId);
        timeline.AddBlocks(blocks.Where(b => b.ClientId == clientId));
        timeline.SortBlocks();

        var entries = new List<ScheduleValidationNotificationDto>();
        ScheduleValidationBuilder.AddCollisions(entries, timeline, string.Empty);
        ScheduleValidationBuilder.AddRestViolations(entries, timeline, string.Empty, policy);
        ScheduleValidationBuilder.AddOvertime(entries, timeline, string.Empty, from, to, policy);
        ScheduleValidationBuilder.AddConsecutiveDays(entries, timeline, string.Empty, from, to, policy);
        ScheduleValidationBuilder.AddWeeklyOvertime(entries, timeline, string.Empty, from, to, policy);
        ScheduleValidationBuilder.AddMinRestDays(entries, timeline, string.Empty, from, to, policy);
        return entries;
    }

    // The key includes CommentParams so distinct same-day collisions (different block pairs) stay
    // separate. Consequence: for AGGREGATE checks (daily/weekly overtime, min-rest-days) the params
    // are bucket totals — worsening an already-violating bucket (e.g. week 52h -> 60h) yields a new
    // key, so it is reported as a NEW conflict even though a violation pre-existed. This is harmless
    // for blocking (only collisions are Error and those key precisely), but a caller using HasAny
    // (find_replacement, P2.1) must decide deliberately how to treat aggregate worsening.
    private static string BuildKey(ScheduleValidationNotificationDto entry)
    {
        var paramSignature = string.Join(
            ",",
            entry.CommentParams.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}"));
        return $"{entry.Comment}|{entry.Date:O}|{entry.ClientId}|{paramSignature}";
    }

    private static Work ToSyntheticWork(PlannedWorkRow row) => new()
    {
        Id = Guid.NewGuid(),
        ClientId = row.ClientId,
        CurrentDate = row.Date,
        StartTime = row.StartTime,
        EndTime = row.EndTime,
        ShiftId = row.ShiftId ?? Guid.Empty,
        ParentWorkId = null,
        AnalyseToken = null
    };

    private async Task<List<Work>> LoadWorksAsync(
        Guid clientId,
        DateOnly from,
        DateOnly to,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        return await _context.Work
            .AsNoTracking()
            .Where(w => w.ClientId == clientId
                && w.CurrentDate >= from
                && w.CurrentDate <= to
                && !w.IsDeleted
                && w.ParentWorkId == null
                && w.AnalyseToken == analyseToken)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<WorkChange>> LoadWorkChangesAsync(
        List<Guid> workIds,
        CancellationToken cancellationToken)
    {
        if (workIds.Count == 0)
        {
            return [];
        }

        return await _context.WorkChange
            .AsNoTracking()
            .Where(wc => workIds.Contains(wc.WorkId) && !wc.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<Break>> LoadBreaksAsync(
        Guid clientId,
        DateOnly from,
        DateOnly to,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        return await _context.Break
            .AsNoTracking()
            .Where(b => b.ClientId == clientId
                && b.CurrentDate >= from
                && b.CurrentDate <= to
                && !b.IsDeleted
                && b.ParentWorkId == null
                && b.AnalyseToken == analyseToken)
            .ToListAsync(cancellationToken);
    }
}
