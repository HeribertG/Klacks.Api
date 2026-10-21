// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Conflict checks before an accept: refuses a scenario whose works sit on a scenario shift without a real root, whose
/// source shifts no longer exist or were deleted, or whose source shift subtree changed after the scenario was created
/// (a concurrent cut by another user). Each refusal is a ConflictException (409).
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>

using Klacks.Api.Application.Exceptions;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioAcceptConflictValidator
{
    private const string RootlessScenarioShiftMessage =
        "Cannot accept scenario: works are planned on scenario shifts that have no shift in the original plan: {0}.";

    private readonly DataBaseContext _context;

    public ScenarioAcceptConflictValidator(DataBaseContext context)
    {
        _context = context;
    }

    public async Task ValidateNoAcceptConflictsAsync(Guid token, CancellationToken ct)
    {
        await EnsureNoRootlessScenarioShiftInUseAsync(token, ct);

        var clones = await _context.Shift.IgnoreQueryFilters()
            .Where(s => s.AnalyseToken == token && !s.IsDeleted && s.ScenarioSourceShiftId.HasValue)
            .Select(s => new { s.Id, SourceId = s.ScenarioSourceShiftId!.Value, s.SourceChildCountSnapshot })
            .ToListAsync(ct);

        if (clones.Count == 0) return;

        var sourceIds = clones.Select(c => c.SourceId).Distinct().ToList();

        var existingSources = await _context.Shift.IgnoreQueryFilters()
            .Where(s => sourceIds.Contains(s.Id))
            .Select(s => new { s.Id, s.IsDeleted })
            .ToListAsync(ct);

        var sourcesById = existingSources.ToDictionary(s => s.Id, s => s.IsDeleted);

        var missing = sourceIds.Where(id => !sourcesById.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            throw new ConflictException(
                $"Cannot accept scenario: source shifts no longer exist: {string.Join(", ", missing)}.");
        }

        var deleted = sourceIds.Where(id => sourcesById[id]).ToList();
        if (deleted.Count > 0)
        {
            throw new ConflictException(
                $"Cannot accept scenario: source shifts have been deleted: {string.Join(", ", deleted)}.");
        }

        var currentChildCounts = await _context.Shift.IgnoreQueryFilters()
            .Where(s => !s.IsDeleted
                && s.AnalyseToken == null
                && s.ParentId.HasValue
                && sourceIds.Contains(s.ParentId.Value))
            .GroupBy(s => s.ParentId!.Value)
            .Select(g => new { ParentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ParentId, x => x.Count, ct);

        foreach (var clone in clones)
        {
            var snapshot = clone.SourceChildCountSnapshot ?? 0;
            var current = currentChildCounts.GetValueOrDefault(clone.SourceId, 0);
            if (snapshot != current)
            {
                throw new ConflictException(
                    $"Cannot accept scenario: source shift {clone.SourceId} subtree changed after scenario creation (children snapshot={snapshot}, current={current}). Likely a concurrent cut by another user.");
            }
        }
    }

    /// <summary>
    /// Refuses to accept a scenario whose works sit on a scenario shift without a real root: promote would leave those
    /// works on a shift it soft-deletes. No current write path creates such a shift; the guard keeps it a clean 409
    /// instead of orphaned real works should one appear.
    /// </summary>
    private async Task EnsureNoRootlessScenarioShiftInUseAsync(Guid token, CancellationToken ct)
    {
        var rootlessShiftIds = await _context.Shift.IgnoreQueryFilters()
            .Where(s => s.AnalyseToken == token && !s.IsDeleted && s.ScenarioSourceShiftId == null)
            .Select(s => s.Id)
            .ToListAsync(ct);
        if (rootlessShiftIds.Count == 0)
        {
            return;
        }

        var usedShiftIds = await _context.Work.IgnoreQueryFilters()
            .Where(w => w.AnalyseToken == token && !w.IsDeleted && rootlessShiftIds.Contains(w.ShiftId))
            .Select(w => w.ShiftId)
            .Distinct()
            .ToListAsync(ct);
        if (usedShiftIds.Count > 0)
        {
            throw new ConflictException(string.Format(RootlessScenarioShiftMessage, string.Join(", ", usedShiftIds)));
        }
    }
}
