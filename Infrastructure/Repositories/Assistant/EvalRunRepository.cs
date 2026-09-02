// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core repository for EvalRun records produced by goldset evaluation jobs.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class EvalRunRepository : IEvalRunRepository
{
    private const int LatestPerModelScanLimit = 500;

    private readonly DataBaseContext _context;

    public EvalRunRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AddAsync(EvalRun record, CancellationToken cancellationToken = default)
    {
        await _context.EvalRuns.AddAsync(record, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<EvalRun?> GetLatestAsync(string goldset, CancellationToken cancellationToken = default)
    {
        return await _context.EvalRuns
            .Where(r => r.Goldset == goldset)
            .OrderByDescending(r => r.CreateTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<EvalRun?> GetLatestAsync(string goldset, string model, CancellationToken cancellationToken = default)
    {
        return await _context.EvalRuns
            .Where(r => r.Goldset == goldset && r.Model == model)
            .OrderByDescending(r => r.CreateTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<EvalRun?> GetBestBaselineAsync(
        string goldset,
        string model,
        int itemsTotal,
        int scorerVersion,
        CancellationToken cancellationToken = default)
    {
        return await _context.EvalRuns
            .Where(r => r.Goldset == goldset
                && r.Model == model
                && r.ItemsTotal == itemsTotal
                && r.ScorerVersion == scorerVersion
                && !r.IsPartial
                && r.ItemsTotal > 0)
            .OrderByDescending(r => r.CompositeScore)
            .ThenByDescending(r => r.CreateTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<EvalRun>> GetLatestPerModelAsync(string goldset, CancellationToken cancellationToken = default)
    {
        var runs = await _context.EvalRuns
            .Where(r => r.Goldset == goldset && r.Model != null)
            .OrderByDescending(r => r.CreateTime)
            .Take(LatestPerModelScanLimit)
            .ToListAsync(cancellationToken);

        return runs
            .GroupBy(r => r.Model, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    public async Task<List<EvalRun>> GetHistoryAsync(string goldset, int limit, CancellationToken cancellationToken = default)
    {
        return await _context.EvalRuns
            .Where(r => r.Goldset == goldset)
            .OrderByDescending(r => r.CreateTime)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
