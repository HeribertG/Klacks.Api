// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core repository for the frozen routing expectations, self-committing.
/// The holdout read spends its budget on the cases of the skill under change first, then on the remaining
/// shipped goldset cases, and only then on the learned ones. OnBeforeSaving stamps CreateTime on insert, so
/// the goldset cases seeded once at startup are the oldest holdout rows in the table forever - a plain
/// newest-first window dropped exactly the curated population out of the gate as soon as enough
/// cluster-born cases existed, while the count check that guards the gate stayed green because it counts
/// rows the replay no longer sees.
/// The goldset half is ordered in memory with an ordinal comparer rather than in SQL, because ORDER BY on a
/// text column follows the database collation and would order the same rows differently on another server -
/// a budget this small must not depend on where it runs.
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
        int limit, string? prioritisedSkillName, CancellationToken cancellationToken = default)
    {
        var goldset = await _context.SkillLearningGoldenCases
            .AsNoTracking()
            .Where(c => c.Partition == GoldenCasePartitions.Holdout
                && c.Origin == GoldenCaseOrigins.Goldset)
            .ToListAsync(cancellationToken);

        var learned = await _context.SkillLearningGoldenCases
            .AsNoTracking()
            .Where(c => c.Partition == GoldenCasePartitions.Holdout
                && c.Origin != GoldenCaseOrigins.Goldset)
            .OrderBy(c => c.ExpectedSourceId == prioritisedSkillName ? 0 : 1)
            .ThenByDescending(c => c.CreateTime)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var goldsetByQuery = goldset.OrderBy(c => c.Query, StringComparer.Ordinal).ToList();

        var ordered = goldsetByQuery.Where(c => IsPrioritised(c, prioritisedSkillName))
            .Concat(learned.Where(c => IsPrioritised(c, prioritisedSkillName)))
            .Concat(goldsetByQuery.Where(c => !IsPrioritised(c, prioritisedSkillName)))
            .Concat(learned.Where(c => !IsPrioritised(c, prioritisedSkillName)));

        return [.. ordered.Take(limit)];
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

    private static bool IsPrioritised(SkillLearningGoldenCase goldenCase, string? prioritisedSkillName) =>
        prioritisedSkillName != null
            && string.Equals(goldenCase.ExpectedSourceId, prioritisedSkillName, StringComparison.Ordinal);
}
