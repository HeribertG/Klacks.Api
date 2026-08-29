// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core repository for the occurrences behind a learning cluster, self-committing.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class SkillLearningCaseRepository : ISkillLearningCaseRepository
{
    private readonly DataBaseContext _context;

    public SkillLearningCaseRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SkillLearningCase learningCase, CancellationToken cancellationToken = default)
    {
        await _context.SkillLearningCases.AddAsync(learningCase, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountDistinctUsersAsync(Guid clusterId, CancellationToken cancellationToken = default)
    {
        return await _context.SkillLearningCases
            .AsNoTracking()
            .Where(c => c.ClusterId == clusterId)
            .Select(c => c.UserId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, int>> CountBySignalAsync(
        Guid clusterId, CancellationToken cancellationToken = default)
    {
        var counts = await _context.SkillLearningCases
            .AsNoTracking()
            .Where(c => c.ClusterId == clusterId)
            .GroupBy(c => c.Signal)
            .Select(group => new { Signal = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(entry => entry.Signal, entry => entry.Count, StringComparer.Ordinal);
    }

    public async Task<bool> HasCaseSinceAsync(
        Guid clusterId, string? userId, DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        return await _context.SkillLearningCases
            .AsNoTracking()
            .AnyAsync(
                c => c.ClusterId == clusterId && c.UserId == userId && c.OccurredAtUtc >= sinceUtc,
                cancellationToken);
    }

    public async Task<IReadOnlyList<SkillLearningCase>> ListByClusterAsync(
        Guid clusterId, int limit, CancellationToken cancellationToken = default)
    {
        return await _context.SkillLearningCases
            .AsNoTracking()
            .Where(c => c.ClusterId == clusterId)
            .OrderByDescending(c => c.OccurredAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountSinceAsync(
        Guid clusterId, DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        return await _context.SkillLearningCases
            .AsNoTracking()
            .CountAsync(c => c.ClusterId == clusterId && c.OccurredAtUtc >= sinceUtc, cancellationToken);
    }

    public async Task<string?> FindExpectedSkillByTrajectoryAsync(
        Guid trajectoryId, CancellationToken cancellationToken = default)
    {
        return await _context.SkillLearningCases
            .AsNoTracking()
            .Where(c => c.TrajectoryId == trajectoryId && c.ExpectedSkill != null)
            .OrderByDescending(c => c.OccurredAtUtc)
            .Select(c => c.ExpectedSkill)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
