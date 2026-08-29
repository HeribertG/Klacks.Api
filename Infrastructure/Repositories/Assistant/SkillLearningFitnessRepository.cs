// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core repository for the weekly fitness snapshots, self-committing. The upsert is a read followed
/// by an update rather than a conditional insert: the unique index is on (candidate, week), the pass
/// runs on a timer with a process-wide gate, and two instances writing the same week would produce the
/// same numbers anyway.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class SkillLearningFitnessRepository : ISkillLearningFitnessRepository
{
    private readonly DataBaseContext _context;

    public SkillLearningFitnessRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task UpsertAsync(SkillLearningFitness fitness, CancellationToken cancellationToken = default)
    {
        var existing = await _context.SkillLearningFitness
            .FirstOrDefaultAsync(
                f => f.CandidateId == fitness.CandidateId && f.WindowStartUtc == fitness.WindowStartUtc,
                cancellationToken);

        if (existing == null)
        {
            await _context.SkillLearningFitness.AddAsync(fitness, cancellationToken);
        }
        else
        {
            existing.Uses = fitness.Uses;
            existing.Successes = fitness.Successes;
            existing.Failures = fitness.Failures;
            existing.Helpful = fitness.Helpful;
            existing.Corrections = fitness.Corrections;
            existing.Recurrences = fitness.Recurrences;
            existing.LastUsedAtUtc = fitness.LastUsedAtUtc;
            existing.Quote = fitness.Quote;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SkillLearningFitness?> GetLatestAsync(
        Guid candidateId, CancellationToken cancellationToken = default)
    {
        return await _context.SkillLearningFitness
            .AsNoTracking()
            .Where(f => f.CandidateId == candidateId)
            .OrderByDescending(f => f.WindowStartUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, SkillLearningFitness>> GetLatestForCandidatesAsync(
        IReadOnlyList<Guid> candidateIds, CancellationToken cancellationToken = default)
    {
        if (candidateIds.Count == 0)
        {
            return new Dictionary<Guid, SkillLearningFitness>();
        }

        var rows = await _context.SkillLearningFitness
            .AsNoTracking()
            .Where(f => candidateIds.Contains(f.CandidateId))
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(f => f.CandidateId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(f => f.WindowStartUtc).First());
    }
}
