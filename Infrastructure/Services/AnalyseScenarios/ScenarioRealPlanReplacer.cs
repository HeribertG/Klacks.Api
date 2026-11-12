// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Delete step of an accept: soft-deletes the real works (with their work changes and expenses), breaks and schedule
/// notes in the accept scope that the scenario replaces, and leaves in place what the day-lock plan keeps - locked rows,
/// rows on a locked day, kept slots and the pauses of kept works. Everything is staged; the accept handler commits. The
/// selection of the deleted works and breaks (SelectDeletionAsync) is read-only and shared with the absence guard that runs
/// before the delete.
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>
/// <param name="scopeResolver">Resolves the real shifts and clients the accept replaces</param>
/// <param name="dayLockPlanBuilder">Builds the day-lock plan of the accept</param>

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioRealPlanReplacer
{
    private readonly DataBaseContext _context;
    private readonly ScenarioAcceptScopeResolver _scopeResolver;
    private readonly AcceptDayLockPlanBuilder _dayLockPlanBuilder;

    public ScenarioRealPlanReplacer(
        DataBaseContext context, ScenarioAcceptScopeResolver scopeResolver, AcceptDayLockPlanBuilder dayLockPlanBuilder)
    {
        _context = context;
        _scopeResolver = scopeResolver;
        _dayLockPlanBuilder = dayLockPlanBuilder;
    }

    public async Task SoftDeleteRealScheduleDataAsync(Guid? groupId, Guid token, DateOnly fromDate, DateOnly untilDate, CancellationToken ct)
    {
        var deletion = await SelectDeletionAsync(groupId, token, fromDate, untilDate, ct);
        var now = DateTime.UtcNow;

        foreach (var work in deletion.Works)
        {
            work.IsDeleted = true;
            work.DeletedTime = now;
        }

        await SoftDeleteRealWorkChildrenAsync(deletion.Works.Select(w => w.Id).ToList(), ct);

        foreach (var brk in deletion.Breaks)
        {
            brk.IsDeleted = true;
            brk.DeletedTime = now;
        }

        await SoftDeleteRealScheduleNotes(deletion.ClientIds, fromDate, untilDate, deletion.Plan, ct);
    }

    /// <summary>
    /// Read-only selection of the real works and breaks the accept deletes; nothing is changed. The movable works of the
    /// scope go, a locked work, a work on a locked day, a work replacing with an employee whose day is locked and a work
    /// whose slot the day-lock plan keeps survive. The real breaks of the scope go, except locked breaks, breaks on a
    /// locked day and pauses of a work the accept keeps.
    /// </summary>
    internal async Task<RealPlanDeletion> SelectDeletionAsync(
        Guid? groupId, Guid token, DateOnly fromDate, DateOnly untilDate, CancellationToken ct)
    {
        var (shiftIds, clientIds) = await _scopeResolver.ResolveRealAcceptScopeAsync(groupId, token, fromDate, untilDate, ct);

        var realWorks = await LoadRealWorksInScopeAsync(shiftIds, fromDate, untilDate, ct);
        var plan = await _dayLockPlanBuilder.BuildAcceptDayLockPlanAsync(
            token, fromDate, untilDate, realWorks.Select(w => w.ClientId).Concat(clientIds ?? []), realWorks.Select(w => w.Id), ct);

        var keptWorkIds = plan.KeptRealWorkIds.ToHashSet();
        var deletedWorks = new List<Work>();
        foreach (var work in realWorks)
        {
            if (plan.LeavesRealWork(work.Id, work.ClientId, work.CurrentDate, work.LockLevel))
            {
                keptWorkIds.Add(work.Id);
            }
            else
            {
                deletedWorks.Add(work);
            }
        }

        var deletedBreaks = await SelectRealBreaksAsync(clientIds, fromDate, untilDate, plan, keptWorkIds, ct);

        return new RealPlanDeletion(clientIds, plan, deletedWorks, deletedBreaks);
    }

    private async Task<List<Work>> LoadRealWorksInScopeAsync(List<Guid>? shiftIds, DateOnly fromDate, DateOnly untilDate, CancellationToken ct)
    {
        IQueryable<Work> workQuery = _context.Work
            .Where(w => !w.IsDeleted && w.AnalyseToken == null
                && w.CurrentDate >= fromDate && w.CurrentDate <= untilDate);

        if (shiftIds != null)
        {
            workQuery = workQuery.Where(w => shiftIds.Contains(w.ShiftId));
        }

        return await workQuery.ToListAsync(ct);
    }

    private async Task SoftDeleteRealWorkChildrenAsync(List<Guid> deletedWorkIds, CancellationToken ct)
    {
        if (deletedWorkIds.Count == 0)
        {
            return;
        }

        var workChanges = await _context.WorkChange
            .Where(wc => !wc.IsDeleted && deletedWorkIds.Contains(wc.WorkId))
            .ToListAsync(ct);

        var expenses = await _context.Expenses
            .Where(e => !e.IsDeleted && deletedWorkIds.Contains(e.WorkId))
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var wc in workChanges) { wc.IsDeleted = true; wc.DeletedTime = now; }
        foreach (var exp in expenses) { exp.IsDeleted = true; exp.DeletedTime = now; }
    }

    private async Task<List<Break>> SelectRealBreaksAsync(
        List<Guid>? clientIds, DateOnly fromDate, DateOnly untilDate, AcceptDayLockPlan plan, HashSet<Guid> keptWorkIds, CancellationToken ct)
    {
        IQueryable<Break> breakQuery = _context.Break
            .Where(b => !b.IsDeleted && b.AnalyseToken == null
                && b.CurrentDate >= fromDate && b.CurrentDate <= untilDate);

        if (clientIds != null)
        {
            breakQuery = breakQuery.Where(b => clientIds.Contains(b.ClientId));
        }

        var realBreaks = await breakQuery.ToListAsync(ct);

        return realBreaks
            .Where(brk => !plan.LeavesRealBreak(brk.LockLevel, brk.ClientId, brk.CurrentDate, brk.ParentWorkId, keptWorkIds))
            .ToList();
    }

    private async Task SoftDeleteRealScheduleNotes(
        List<Guid>? clientIds, DateOnly fromDate, DateOnly untilDate, AcceptDayLockPlan plan, CancellationToken ct)
    {
        IQueryable<ScheduleNote> noteQuery = _context.ScheduleNotes
            .Where(sn => !sn.IsDeleted && sn.AnalyseToken == null
                && sn.CurrentDate >= fromDate && sn.CurrentDate <= untilDate);

        if (clientIds != null)
        {
            noteQuery = noteQuery.Where(sn => clientIds.Contains(sn.ClientId));
        }

        var realNotes = await noteQuery.ToListAsync(ct);
        var now = DateTime.UtcNow;

        foreach (var note in realNotes.Where(n => !plan.IsLocked(n.ClientId, n.CurrentDate)))
        {
            note.IsDeleted = true;
            note.DeletedTime = now;
        }
    }
}
