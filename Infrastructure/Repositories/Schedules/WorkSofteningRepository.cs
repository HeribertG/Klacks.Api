// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

/// <summary>
/// EF Core implementation of <see cref="IWorkSofteningRepository"/>. Uses
/// IS NOT DISTINCT FROM-style null comparisons for AnalyseToken to keep main-scenario
/// (token=null) and scenario rows correctly isolated.
/// </summary>
/// <param name="context">EF Core database context</param>
public sealed class WorkSofteningRepository : IWorkSofteningRepository
{
    private readonly DataBaseContext _context;

    public WorkSofteningRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<WorkSoftening>> LoadAsync(
        IEnumerable<Guid> agentIds,
        DateOnly fromDate,
        DateOnly untilDate,
        Guid? analyseToken,
        CancellationToken ct)
    {
        var ids = agentIds.ToList();
        return await _context.WorkSoftening
            .AsNoTracking()
            .Where(s => ids.Contains(s.ClientId)
                        && s.CurrentDate >= fromDate
                        && s.CurrentDate <= untilDate
                        && (s.AnalyseToken == analyseToken || (s.AnalyseToken == null && analyseToken == null)))
            .ToListAsync(ct);
    }

    public async Task ReplaceForRangeAsync(
        IReadOnlyList<Guid> agentIds,
        DateOnly fromDate,
        DateOnly untilDate,
        Guid? analyseToken,
        IReadOnlyList<WorkSoftening> newRows,
        CancellationToken ct)
    {
        var existing = await _context.WorkSoftening
            .Where(s => agentIds.Contains(s.ClientId)
                        && s.CurrentDate >= fromDate
                        && s.CurrentDate <= untilDate
                        && (s.AnalyseToken == analyseToken || (s.AnalyseToken == null && analyseToken == null)))
            .ToListAsync(ct);

        _context.WorkSoftening.RemoveRange(existing);

        if (newRows.Count > 0)
        {
            await _context.WorkSoftening.AddRangeAsync(newRows, ct);
        }
    }

    public async Task DeleteByAnalyseTokenAsync(Guid? analyseToken, CancellationToken ct)
    {
        var existing = await _context.WorkSoftening
            .Where(s => s.AnalyseToken == analyseToken || (s.AnalyseToken == null && analyseToken == null))
            .ToListAsync(ct);

        _context.WorkSoftening.RemoveRange(existing);
    }

    public async Task DeleteByRangeAndTokenAsync(
        DateOnly fromDate,
        DateOnly untilDate,
        Guid? analyseToken,
        CancellationToken ct)
    {
        var existing = await _context.WorkSoftening
            .Where(s => s.CurrentDate >= fromDate
                        && s.CurrentDate <= untilDate
                        && (s.AnalyseToken == analyseToken || (s.AnalyseToken == null && analyseToken == null)))
            .ToListAsync(ct);

        _context.WorkSoftening.RemoveRange(existing);
    }
}
