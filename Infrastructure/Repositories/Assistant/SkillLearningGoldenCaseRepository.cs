// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core repository for the frozen routing expectations, self-committing.
/// The holdout read spends its budget on the shipped goldset cases first and only then on the learned
/// ones. OnBeforeSaving stamps CreateTime on insert, so the goldset cases seeded once at startup are the
/// oldest holdout rows in the table forever - a plain newest-first window dropped exactly the curated
/// population out of the gate as soon as enough cluster-born cases existed, while the count check that
/// guards the gate stayed green because it counts rows the replay no longer sees.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class SkillLearningGoldenCaseRepository : ISkillLearningGoldenCaseRepository
{
    private readonly DataBaseContext _context;

    public SkillLearningGoldenCaseRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SkillLearningGoldenCase goldenCase, CancellationToken cancellationToken = default)
    {
        await _context.SkillLearningGoldenCases.AddAsync(goldenCase, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(
        IReadOnlyList<SkillLearningGoldenCase> goldenCases, CancellationToken cancellationToken = default)
    {
        if (goldenCases.Count == 0)
        {
            return;
        }

        await _context.SkillLearningGoldenCases.AddRangeAsync(goldenCases, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRangeAsync(
        IReadOnlyList<SkillLearningGoldenCase> goldenCases, CancellationToken cancellationToken = default)
    {
        if (goldenCases.Count == 0)
        {
            return;
        }

        _context.SkillLearningGoldenCases.UpdateRange(goldenCases);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SkillLearningGoldenCase>> ListHoldoutAsync(
        int limit, CancellationToken cancellationToken = default)
    {
        var goldset = await _context.SkillLearningGoldenCases
            .AsNoTracking()
            .Where(c => c.Partition == GoldenCasePartitions.Holdout
                && c.Origin == GoldenCaseOrigins.Goldset)
            .OrderByDescending(c => c.CreateTime)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var remaining = limit - goldset.Count;
        if (remaining <= 0)
        {
            return goldset;
        }

        var learned = await _context.SkillLearningGoldenCases
            .AsNoTracking()
            .Where(c => c.Partition == GoldenCasePartitions.Holdout
                && c.Origin != GoldenCaseOrigins.Goldset)
            .OrderByDescending(c => c.CreateTime)
            .Take(remaining)
            .ToListAsync(cancellationToken);

        return [.. goldset, .. learned];
    }

    public async Task<int> CountHoldoutAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SkillLearningGoldenCases
            .AsNoTracking()
            .CountAsync(c => c.Partition == GoldenCasePartitions.Holdout, cancellationToken);
    }

    public async Task<IReadOnlyList<SkillLearningGoldenCase>> ListByOriginAsync(
        string origin, CancellationToken cancellationToken = default)
    {
        return await _context.SkillLearningGoldenCases
            .AsNoTracking()
            .Where(c => c.Origin == origin)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        string query, string expectedSourceId, CancellationToken cancellationToken = default)
    {
        return await _context.SkillLearningGoldenCases
            .AsNoTracking()
            .AnyAsync(c => c.Query == query && c.ExpectedSourceId == expectedSourceId, cancellationToken);
    }
}
