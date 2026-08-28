// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core repository for the learning clusters, self-committing. Every write that two API instances can
/// reach at the same moment is expressed as a single statement - an insert guarded by the partial unique
/// index, or a conditional ExecuteUpdate - so no counter and no state transition can be lost between a
/// read and a write.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class SkillLearningClusterRepository : ISkillLearningClusterRepository
{
    private const string UniqueViolationSqlState = "23505";

    private readonly DataBaseContext _context;

    public SkillLearningClusterRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<SkillLearningCluster?> FindByKeyAsync(
        Guid agentId, string clusterKey, CancellationToken cancellationToken = default)
    {
        return await _context.SkillLearningClusters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.AgentId == agentId && c.ClusterKey == clusterKey, cancellationToken);
    }

    public async Task<SkillLearningCluster?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SkillLearningClusters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<bool> TryInsertAsync(SkillLearningCluster cluster, CancellationToken cancellationToken = default)
    {
        await _context.SkillLearningClusters.AddAsync(cluster, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            Detach(cluster);
            return false;
        }
        catch
        {
            Detach(cluster);
            throw;
        }
    }

    public async Task RegisterOccurrenceAsync(
        Guid id,
        DateTime seenAtUtc,
        int distinctUserCount,
        string signalKindsJson,
        CancellationToken cancellationToken = default)
    {
        await _context.SkillLearningClusters
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(c => c.OccurrenceCount, c => c.OccurrenceCount + 1)
                    .SetProperty(c => c.DistinctUserCount, distinctUserCount)
                    .SetProperty(c => c.SignalKindsJson, signalKindsJson)
                    .SetProperty(c => c.LastSeenAtUtc, seenAtUtc)
                    .SetProperty(c => c.UpdateTime, seenAtUtc),
                cancellationToken);
    }

    public async Task<bool> TryTransitionAsync(
        Guid id, string fromStatus, string toStatus, CancellationToken cancellationToken = default)
    {
        if (!SkillLearningStateMachine.IsLegalTransition(fromStatus, toStatus))
        {
            throw new InvalidOperationException(
                $"Illegal learning cluster transition '{fromStatus}' to '{toStatus}'.");
        }

        var now = DateTime.UtcNow;
        var affected = await _context.SkillLearningClusters
            .Where(c => c.Id == id && c.Status == fromStatus)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(c => c.Status, toStatus)
                    .SetProperty(c => c.StatusChangedAtUtc, now)
                    .SetProperty(c => c.UpdateTime, now),
                cancellationToken);

        return affected > 0;
    }

    public async Task<int> PromoteReadyAsync(
        int minOccurrences,
        int minDistinctUsers,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.SkillLearningClusters
            .Where(c => c.Status == SkillLearningClusterStatuses.Collecting
                && (c.OccurrenceCount >= minOccurrences || c.DistinctUserCount >= minDistinctUsers))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(c => c.Status, SkillLearningClusterStatuses.Ready)
                    .SetProperty(c => c.StatusChangedAtUtc, now)
                    .SetProperty(c => c.UpdateTime, now),
                cancellationToken);
    }

    public async Task<IReadOnlyList<SkillLearningCluster>> ListByStatusAsync(
        IReadOnlyList<string> statuses,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await _context.SkillLearningClusters
            .AsNoTracking()
            .Where(c => statuses.Contains(c.Status))
            .OrderByDescending(c => c.OccurrenceCount)
            .ThenByDescending(c => c.LastSeenAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, int>> CountByStatusInWindowAsync(
        IReadOnlyList<string> statuses,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var counts = await _context.SkillLearningClusters
            .AsNoTracking()
            .Where(c => statuses.Contains(c.Status)
                && c.StatusChangedAtUtc >= fromUtc
                && c.StatusChangedAtUtc < toUtc)
            .GroupBy(c => c.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(entry => entry.Status, entry => entry.Count, StringComparer.Ordinal);
    }

    public async Task<int> SoftDeleteTerminalOlderThanAsync(
        DateTime thresholdUtc, CancellationToken cancellationToken = default)
    {
        var terminal = SkillLearningStateMachine.TerminalStatuses;
        var now = DateTime.UtcNow;

        return await _context.SkillLearningClusters
            .Where(c => terminal.Contains(c.Status) && c.LastSeenAtUtc < thresholdUtc)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(c => c.IsDeleted, true)
                    .SetProperty(c => c.DeletedTime, now),
                cancellationToken);
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        (exception.InnerException as PostgresException)?.SqlState == UniqueViolationSqlState;

    private void Detach(params object[] entities)
    {
        foreach (var entity in entities)
        {
            _context.Entry(entity).State = EntityState.Detached;
        }
    }
}
