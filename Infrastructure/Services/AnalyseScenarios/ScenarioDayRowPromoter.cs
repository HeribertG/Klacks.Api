// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Promotes the scenario breaks and schedule notes of an accept into the real plan, following the day-lock plan: rows on
/// a locked day (and pauses of a discarded scenario work) are discarded instead, boundary rows are discarded, and each
/// returns how many discarded rows the real plan does not hold an identical copy of.
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioDayRowPromoter
{
    private readonly DataBaseContext _context;

    public ScenarioDayRowPromoter(DataBaseContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Promotes the scenario breaks of the period, except those on a locked day, those pausing a discarded scenario work,
    /// and copies of an absence the real plan keeps as a locked break (the real one survives the delete, so promoting
    /// the copy would duplicate it). Boundary breaks are discarded as before. Returns how many discarded breaks have no
    /// identical real break kept on their day.
    /// </summary>
    internal async Task<int> PromoteScenarioBreaksAsync(
        Guid token, DateOnly fromDate, DateOnly untilDate, AcceptDayLockPlan plan, DateTime now, CancellationToken ct)
    {
        var scenarioBreaks = await _context.Break.IgnoreQueryFilters()
            .Where(b => b.AnalyseToken == token && !b.IsDeleted).ToListAsync(ct);

        var clientIds = scenarioBreaks.Select(b => b.ClientId).Distinct().ToList();
        var realBreaks = clientIds.Count == 0
            ? new List<AcceptBreakSnapshot>()
            : await _context.Break.IgnoreQueryFilters()
                .AsNoTracking()
                .Where(b => b.AnalyseToken == null && !b.IsDeleted
                    && b.CurrentDate >= fromDate && b.CurrentDate <= untilDate
                    && clientIds.Contains(b.ClientId))
                .Select(AcceptRowSnapshots.Break)
                .ToListAsync(ct);

        var lockedRealAbsences = realBreaks
            .Where(b => b.LockLevel != WorkLockLevel.None)
            .Select(b => (b.ClientId, b.CurrentDate, b.AbsenceId))
            .ToHashSet();
        var keptParentIds = realBreaks
            .Where(b => b.ParentWorkId is { } parentId && (plan.KeepsRealWork(parentId) || plan.IsWorkChangeLocked(parentId)))
            .Select(b => b.ParentWorkId!.Value)
            .ToHashSet();
        var keptRealBreaks = realBreaks
            .Where(b => plan.LeavesRealBreak(b.LockLevel, b.ClientId, b.CurrentDate, b.ParentWorkId, keptParentIds))
            .Select(b => (b.ClientId, b.CurrentDate, b.AbsenceId, b.StartTime, b.EndTime))
            .ToHashSet();

        var skipped = 0;
        foreach (var b in scenarioBreaks)
        {
            if (b.CurrentDate < fromDate || b.CurrentDate > untilDate)
            {
                b.IsDeleted = true;
                b.DeletedTime = now;
            }
            else if (plan.IsLocked(b.ClientId, b.CurrentDate)
                || (b.ParentWorkId is { } parentId && plan.DiscardsClone(parentId))
                || lockedRealAbsences.Contains((b.ClientId, b.CurrentDate, b.AbsenceId)))
            {
                b.IsDeleted = true;
                b.DeletedTime = now;
                if (!keptRealBreaks.Contains((b.ClientId, b.CurrentDate, b.AbsenceId, b.StartTime, b.EndTime)))
                {
                    skipped++;
                }
            }
            else
            {
                b.AnalyseToken = null;
            }
        }

        return skipped;
    }

    /// <summary>
    /// Promotes the scenario schedule notes of the period, except those on a locked day (the real notes there survive the
    /// delete). Boundary notes are discarded as before. Returns how many discarded notes have no identical real note on
    /// their day.
    /// </summary>
    internal async Task<int> PromoteScenarioNotesAsync(
        Guid token, DateOnly fromDate, DateOnly untilDate, AcceptDayLockPlan plan, DateTime now, CancellationToken ct)
    {
        var scenarioNotes = await _context.ScheduleNotes.IgnoreQueryFilters()
            .Where(sn => sn.AnalyseToken == token && !sn.IsDeleted).ToListAsync(ct);

        var lockedClientIds = scenarioNotes
            .Where(sn => sn.CurrentDate >= fromDate && sn.CurrentDate <= untilDate && plan.IsLocked(sn.ClientId, sn.CurrentDate))
            .Select(sn => sn.ClientId)
            .Distinct()
            .ToList();
        var keptRealNotes = lockedClientIds.Count == 0
            ? new HashSet<(Guid ClientId, DateOnly CurrentDate, string Content)>()
            : (await _context.ScheduleNotes.IgnoreQueryFilters()
                .AsNoTracking()
                .Where(sn => sn.AnalyseToken == null && !sn.IsDeleted
                    && sn.CurrentDate >= fromDate && sn.CurrentDate <= untilDate
                    && lockedClientIds.Contains(sn.ClientId))
                .Select(sn => new { sn.ClientId, sn.CurrentDate, sn.Content })
                .ToListAsync(ct))
                .Select(sn => (sn.ClientId, sn.CurrentDate, sn.Content))
                .ToHashSet();

        var skipped = 0;
        foreach (var sn in scenarioNotes)
        {
            if (sn.CurrentDate < fromDate || sn.CurrentDate > untilDate)
            {
                sn.IsDeleted = true;
                sn.DeletedTime = now;
            }
            else if (plan.IsLocked(sn.ClientId, sn.CurrentDate))
            {
                sn.IsDeleted = true;
                sn.DeletedTime = now;
                if (!keptRealNotes.Contains((sn.ClientId, sn.CurrentDate, sn.Content)))
                {
                    skipped++;
                }
            }
            else
            {
                sn.AnalyseToken = null;
            }
        }

        return skipped;
    }
}
