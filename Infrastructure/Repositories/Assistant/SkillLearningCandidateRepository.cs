// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core repository for the generated learning candidates, self-committing. Every verdict is written
/// the moment it is reached, so a run that dies mid-round leaves behind what it already judged instead of
/// making the next run repeat the language model calls.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class SkillLearningCandidateRepository : ISkillLearningCandidateRepository
{
    private readonly DataBaseContext _context;

    public SkillLearningCandidateRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SkillLearningCandidate candidate, CancellationToken cancellationToken = default)
    {
        await _context.SkillLearningCandidates.AddAsync(candidate, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateVerdictAsync(
        Guid id,
        string status,
        string? routingResultJson,
        string? errorText,
        DateTime? activatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        await _context.SkillLearningCandidates
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(c => c.Status, status)
                    .SetProperty(c => c.RoutingResultJson, routingResultJson)
                    .SetProperty(c => c.ErrorText, errorText)
                    .SetProperty(c => c.ActivatedAtUtc, activatedAtUtc)
                    .SetProperty(c => c.UpdateTime, now),
                cancellationToken);
    }

    public async Task<int> CountByClusterAsync(Guid clusterId, CancellationToken cancellationToken = default)
    {
        return await _context.SkillLearningCandidates
            .AsNoTracking()
            .CountAsync(c => c.ClusterId == clusterId, cancellationToken);
    }

    public async Task<IReadOnlyList<SkillLearningCandidate>> ListByClusterAsync(
        Guid clusterId, CancellationToken cancellationToken = default)
    {
        return await _context.SkillLearningCandidates
            .AsNoTracking()
            .Where(c => c.ClusterId == clusterId)
            .OrderBy(c => c.VariantNo)
            .ToListAsync(cancellationToken);
    }
}
