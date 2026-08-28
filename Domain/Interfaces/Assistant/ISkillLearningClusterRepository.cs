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
    /// Takes a ready cluster into a learning round and reports whether this caller won it. Written as one
    /// conditional statement, so two overlapping runs - a tick and a manual trigger, or two instances -
    /// can never work on the same cluster.
    /// </summary>
    /// <param name="instance">Machine name of the claiming instance, stored for diagnosis</param>
    Task<bool> TryClaimForLearningAsync(
        Guid id, string instance, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns clusters whose claim is older than the given moment to ready, so a process that died
    /// mid-round does not park them in learning forever.
    /// </summary>
    Task<int> ReleaseStaleClaimsAsync(DateTime thresholdUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends a learning round: moves the claimed cluster to its outcome status, records what it produced
    /// or why it failed, and drops the claim. Conditional on the cluster still being in learning, so a
    /// stale reclaim that already handed it on cannot be overwritten.
    /// </summary>
    /// <param name="outcomeRefKind">phrase or capability, null when the round produced nothing</param>
    /// <param name="outcomeRef">Id of the created phrase, or name of the created recipe</param>
    /// <param name="lastError">Why the round failed, kept as the seed of the next round's prompt</param>
    Task<bool> FinishLearningAsync(
        Guid id,
        string toStatus,
        string? outcomeRefKind,
        string? outcomeRef,
        string? lastError,
        int attemptCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hands an unfulfillable cluster back to the learning loop and reports whether it was still
    /// unfulfillable when the write landed. Clears the attempt budget and the recorded error in the same
    /// statement, so a reopened wish starts its next round with a full budget rather than falling out
    /// again on the first attempt.
    /// </summary>
    Task<bool> TryRetryUnfulfillableAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes clusters that are finished business and have gone quiet; the physical delete is left
    /// to the data retention background service. Both clocks must have run out: the status has to have
    /// settled before the given moment AND the cluster must not have been seen since. An unfulfillable
    /// cluster keeps counting recurrences, and ageing it on the status clock alone would throw away the
    /// evidence of a wish people are still asking for - which is precisely the negative fitness signal
    /// stage G3 measures. A cluster that really is finished stops counting, so its last-seen clock runs
    /// out too and it is collected as before.
    /// </summary>
    Task<int> SoftDeleteRetentionEligibleOlderThanAsync(
        DateTime thresholdUtc, CancellationToken cancellationToken = default);
}
