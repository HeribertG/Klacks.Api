// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reject/delete sweep of a scenario: soft-deletes every row of the scenario token - works with their sub-works, work
/// changes and expenses, breaks with the pauses of its works, shifts with their required qualifications, notes,
/// commands, softenings, client shift preferences, shift expenses and memberships (cloned-shift and temporary
/// cross-group alike, so a rejected borrow never leaves a permanent membership behind) - plus the engine's untouched
/// replacement proposals and the scenario's planning constraints. Everything is staged; the caller commits.
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>
/// <param name="replacementRequestBook">Drops the engine's untouched replacement proposals</param>
/// <param name="ownedRowStager">Drops the scenario's planning constraints</param>

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioDataDiscarder
{
    private readonly DataBaseContext _context;
    private readonly ScenarioReplacementRequestBook _replacementRequestBook;
    private readonly ScenarioOwnedRowStager _ownedRowStager;

    public ScenarioDataDiscarder(
        DataBaseContext context, ScenarioReplacementRequestBook replacementRequestBook, ScenarioOwnedRowStager ownedRowStager)
    {
        _context = context;
        _replacementRequestBook = replacementRequestBook;
        _ownedRowStager = ownedRowStager;
    }

    public async Task SoftDeleteScenarioDataAsync(Guid token, CancellationToken ct)
    {
        var workIds = await SoftDeleteWorksWithChildrenAsync(token, ct);
        await SoftDeleteBreaksAsync(token, workIds, ct);
        await SoftDeleteShiftsAsync(token, ct);
        await SoftDeleteTokenRowsAsync(token, ct);
        await _replacementRequestBook.StageProposedReplacementRequestsDeletedAsync(token, DateTime.UtcNow, ct);
        await _ownedRowStager.SoftDeleteScenarioPlanningConstraintsAsync(token, DateTime.UtcNow, ct);
    }

    /// <summary>
    /// Soft-deletes the scenario's works, the sub-works of those works, and the work changes and expenses of both.
    /// Returns the ids of the scenario's own works, whose pauses are swept with the breaks.
    /// </summary>
    private async Task<List<Guid>> SoftDeleteWorksWithChildrenAsync(Guid token, CancellationToken ct)
    {
        var works = await _context.Work.IgnoreQueryFilters()
            .Where(w => w.AnalyseToken == token && !w.IsDeleted).ToListAsync(ct);
        var workIds = works.Select(w => w.Id).ToList();

        var orphanSubWorks = workIds.Count == 0
            ? new List<Work>()
            : await _context.Work.IgnoreQueryFilters()
                .Where(w => !w.IsDeleted
                    && w.ParentWorkId.HasValue
                    && workIds.Contains(w.ParentWorkId.Value))
                .ToListAsync(ct);
        var allWorkIds = workIds.Concat(orphanSubWorks.Select(w => w.Id)).ToList();

        var workChanges = await _context.WorkChange
            .Where(wc => !wc.IsDeleted && allWorkIds.Contains(wc.WorkId)).ToListAsync(ct);
        foreach (var wc in workChanges) { wc.IsDeleted = true; wc.DeletedTime = DateTime.UtcNow; }

        var expenses = await _context.Expenses
            .Where(e => !e.IsDeleted && allWorkIds.Contains(e.WorkId)).ToListAsync(ct);
        foreach (var exp in expenses) { exp.IsDeleted = true; exp.DeletedTime = DateTime.UtcNow; }

        foreach (var w in works) { w.IsDeleted = true; w.DeletedTime = DateTime.UtcNow; }
        foreach (var w in orphanSubWorks) { w.IsDeleted = true; w.DeletedTime = DateTime.UtcNow; }

        return workIds;
    }

    private async Task SoftDeleteBreaksAsync(Guid token, List<Guid> workIds, CancellationToken ct)
    {
        var breaks = await _context.Break.IgnoreQueryFilters()
            .Where(b => b.AnalyseToken == token && !b.IsDeleted).ToListAsync(ct);
        foreach (var b in breaks) { b.IsDeleted = true; b.DeletedTime = DateTime.UtcNow; }

        var orphanSubBreaks = workIds.Count == 0
            ? new List<Break>()
            : await _context.Break.IgnoreQueryFilters()
                .Where(b => !b.IsDeleted
                    && b.ParentWorkId.HasValue
                    && workIds.Contains(b.ParentWorkId.Value))
                .ToListAsync(ct);
        foreach (var b in orphanSubBreaks) { b.IsDeleted = true; b.DeletedTime = DateTime.UtcNow; }
    }

    private async Task SoftDeleteShiftsAsync(Guid token, CancellationToken ct)
    {
        var shifts = await _context.Shift.IgnoreQueryFilters()
            .Where(s => s.AnalyseToken == token && !s.IsDeleted).ToListAsync(ct);
        foreach (var s in shifts) { s.IsDeleted = true; s.DeletedTime = DateTime.UtcNow; }

        var cloneShiftIds = shifts.Select(s => s.Id).ToList();
        if (cloneShiftIds.Count > 0)
        {
            var requiredQualifications = await _context.ShiftRequiredQualification.IgnoreQueryFilters()
                .Where(srq => !srq.IsDeleted && cloneShiftIds.Contains(srq.ShiftId)).ToListAsync(ct);
            foreach (var srq in requiredQualifications) { srq.IsDeleted = true; srq.DeletedTime = DateTime.UtcNow; }
        }
    }

    private async Task SoftDeleteTokenRowsAsync(Guid token, CancellationToken ct)
    {
        var notes = await _context.ScheduleNotes.IgnoreQueryFilters()
            .Where(sn => sn.AnalyseToken == token && !sn.IsDeleted).ToListAsync(ct);
        foreach (var sn in notes) { sn.IsDeleted = true; sn.DeletedTime = DateTime.UtcNow; }

        var commands = await _context.ScheduleCommands.IgnoreQueryFilters()
            .Where(sc => sc.AnalyseToken == token && !sc.IsDeleted).ToListAsync(ct);
        foreach (var sc in commands) { sc.IsDeleted = true; sc.DeletedTime = DateTime.UtcNow; }

        var softenings = await _context.WorkSoftening.IgnoreQueryFilters()
            .Where(ws => ws.AnalyseToken == token && !ws.IsDeleted).ToListAsync(ct);
        foreach (var ws in softenings) { ws.IsDeleted = true; ws.DeletedTime = DateTime.UtcNow; }

        var preferences = await _context.Set<ClientShiftPreference>().IgnoreQueryFilters()
            .Where(p => p.AnalyseToken == token && !p.IsDeleted).ToListAsync(ct);
        foreach (var p in preferences) { p.IsDeleted = true; p.DeletedTime = DateTime.UtcNow; }

        var shiftExpenses = await _context.Set<ShiftExpenses>().IgnoreQueryFilters()
            .Where(e => e.AnalyseToken == token && !e.IsDeleted).ToListAsync(ct);
        foreach (var e in shiftExpenses) { e.IsDeleted = true; e.DeletedTime = DateTime.UtcNow; }

        var memberships = await _context.Set<GroupItem>().IgnoreQueryFilters()
            .Where(gi => gi.AnalyseToken == token && !gi.IsDeleted).ToListAsync(ct);
        foreach (var gi in memberships) { gi.IsDeleted = true; gi.DeletedTime = DateTime.UtcNow; }
    }
}
