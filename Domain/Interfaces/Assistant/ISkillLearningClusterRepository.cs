// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for the learning clusters. Self-committing, like every other assistant repository: its
/// callers are fire-and-forget post-turn hooks and background services that run outside the HTTP request
/// cycle, so each state change must be durable the moment it is made.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillLearningClusterRepository
{
    Task<SkillLearningCluster?> FindByKeyAsync(Guid agentId, string clusterKey, CancellationToken cancellationToken = default);

    Task<SkillLearningCluster?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a cluster and reports whether this caller won. Returns false when the partial unique index
    /// on (agent_id, cluster_key) rejected the row, which is the normal outcome when two instances handle
    /// the same utterance at the same moment - the loser then takes the update path.
    /// </summary>
    Task<bool> TryInsertAsync(SkillLearningCluster cluster, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records one further occurrence: raises the counters, refreshes LastSeenAtUtc and stores the
    /// recomputed signal histogram. Written as a single statement so concurrent turns cannot lose a count.
    /// </summary>
    /// <param name="distinctUserCount">Freshly counted number of different users, denormalised for the threshold</param>
    Task RegisterOccurrenceAsync(
        Guid id,
        DateTime seenAtUtc,
        int distinctUserCount,
        string signalKindsJson,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a cluster from one status to another only if it still holds the expected status, and reports
    /// whether this caller won.
    /// </summary>
    Task<bool> TryTransitionAsync(Guid id, string fromStatus, string toStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Promotes every collecting cluster that reached the threshold to ready and returns how many were
    /// promoted. Both criteria are alternatives: enough repetitions, or enough different people.
    /// </summary>
    Task<int> PromoteReadyAsync(
        int minOccurrences,
        int minDistinctUsers,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SkillLearningCluster>> ListByStatusAsync(
        IReadOnlyList<string> statuses,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// How many clusters entered each of the given statuses inside a half-open time window, the "is there
    /// anything new" question the weekly digest asks before it emits an event. Statuses without a single
    /// cluster in the window are absent from the result.
    /// </summary>
    /// <param name="fromUtc">Inclusive lower bound of the window</param>
    /// <param name="toUtc">Exclusive upper bound of the window</param>
    Task<IReadOnlyDictionary<string, int>> CountByStatusInWindowAsync(
        IReadOnlyList<string> statuses,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes terminal clusters last seen before the given moment; the physical delete is left to
    /// the data retention background service.
    /// </summary>
    Task<int> SoftDeleteTerminalOlderThanAsync(DateTime thresholdUtc, CancellationToken cancellationToken = default);
}
