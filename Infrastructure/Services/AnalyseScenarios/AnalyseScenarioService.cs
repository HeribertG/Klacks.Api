// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core-backed implementation of AnalyseScenario data operations.
/// Owns the DataBaseContext-level bulk operations that command handlers
/// used to perform inline; extracted so Reject and Delete handlers share
/// one source of truth for the soft-delete + orphan sweep.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public class AnalyseScenarioService : IAnalyseScenarioService
{
    private readonly DataBaseContext _context;

    public AnalyseScenarioService(DataBaseContext context)
    {
        _context = context;
    }

    public async Task SoftDeleteScenarioDataAsync(Guid token, CancellationToken ct)
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

        var shifts = await _context.Shift.IgnoreQueryFilters()
            .Where(s => s.AnalyseToken == token && !s.IsDeleted).ToListAsync(ct);
        foreach (var s in shifts) { s.IsDeleted = true; s.DeletedTime = DateTime.UtcNow; }

        var notes = await _context.ScheduleNotes.IgnoreQueryFilters()
            .Where(sn => sn.AnalyseToken == token && !sn.IsDeleted).ToListAsync(ct);
        foreach (var sn in notes) { sn.IsDeleted = true; sn.DeletedTime = DateTime.UtcNow; }
    }
}
