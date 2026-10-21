// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Read-only lookups over the rows of a scenario: the real root of cloned shifts, the shift ids of a scenario together
/// with their roots, and the ids of a scenario's live breaks.
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>

using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioRowLookup
{
    private readonly DataBaseContext _context;

    public ScenarioRowLookup(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyDictionary<Guid, Guid>> ResolveRootShiftIdsAsync(
        IReadOnlyCollection<Guid> shiftIds, CancellationToken cancellationToken)
    {
        if (shiftIds.Count == 0)
        {
            return new Dictionary<Guid, Guid>();
        }

        var ids = shiftIds.Distinct().ToList();
        return await _context.Shift.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => ids.Contains(s.Id) && s.ScenarioSourceShiftId != null)
            .ToDictionaryAsync(s => s.Id, s => s.ScenarioSourceShiftId!.Value, cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> GetScenarioShiftIdsWithRootsAsync(Guid token, CancellationToken cancellationToken)
    {
        var shifts = await _context.Shift.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.AnalyseToken == token && !s.IsDeleted)
            .Select(s => new { s.Id, s.ScenarioSourceShiftId })
            .ToListAsync(cancellationToken);

        var ids = shifts.Select(s => s.Id).ToHashSet();
        ids.UnionWith(shifts.Where(s => s.ScenarioSourceShiftId.HasValue).Select(s => s.ScenarioSourceShiftId!.Value));
        return ids;
    }

    public async Task<IReadOnlyList<Guid>> GetScenarioBreakIdsAsync(Guid token, CancellationToken ct)
    {
        return await _context.Break.IgnoreQueryFilters()
            .Where(b => b.AnalyseToken == token && !b.IsDeleted)
            .Select(b => b.Id)
            .ToListAsync(ct);
    }
}
