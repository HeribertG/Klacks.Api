// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence for the condition ledger (agent_conditions) and its append-only audit history
/// (agent_condition_events). Every status change goes through <see cref="TryTransitionAsync"/>, a
/// conditional UPDATE ... WHERE status = expected, so two API instances scanning the same tick can never
/// both advance the same row.
/// </summary>
/// <remarks>
/// This is the self-committing repository convention (not the stage-only + IUnitOfWork one used by
/// core-domain repositories): the callers are the proactive tick and later the action dispatcher, both
/// running outside the HTTP request cycle, and each ledger step must persist on its own so a crashed
/// tick cannot lose a transition that already happened in the outside world. Never call this repository
/// between a stage-only repository write and its IUnitOfWork.CompleteAsync() - the SaveChangesAsync here
/// flushes the whole shared DbContext, including that pending write.
/// </remarks>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentConditionRepository
{
    /// <summary>
    /// Returns the single open (non-terminal) row carrying this fingerprint, or null. Deliberately not
    /// filtered by TriggerKind: the partial unique index is on Fingerprint alone, so a kind filter would
    /// hide a cross-kind collision from the lookup while the index still rejected the insert, leaving the
    /// caller in an unresolvable retry loop.
    /// </summary>
    Task<AgentCondition?> FindOpenByFingerprintAsync(string fingerprint, CancellationToken cancellationToken = default);

    /// <summary>
    /// The row with this id whatever its status, or null. Unlike <see cref="FindOpenByFingerprintAsync"/>
    /// this deliberately returns terminal rows too: a caller that wants to move a row along needs to see
    /// the status it is actually in before it can pick a compare-and-swap source, and "already terminal"
    /// is an answer it must be able to distinguish from "gone".
    /// </summary>
    Task<AgentCondition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>All open (non-terminal) rows of one detector kind, oldest detection first.</summary>
    Task<List<AgentCondition>> GetOpenByKindAsync(string triggerKind, CancellationToken cancellationToken = default);

    /// <summary>
    /// The AgentConditionPlannerRelevantStatuses.Values rows a planner in this scope may see, up to
    /// <paramref name="take"/>, sorted severity (High before Medium before Low) then
    /// <see cref="AgentCondition.DetectedAtUtc"/> ascending (oldest first) within the same severity.
    /// Deliberately does NOT exclude Escalated: it is a terminal status only for the partial unique index (a
    /// re-arm can open a fresh Detected row for the same fingerprint alongside it), so an Escalated row and a
    /// newer open row of the same underlying condition can legitimately both be in the result at once - that
    /// is the correct picture ("escalated after N attempts, and re-detected since"), not a duplicate to be
    /// collapsed. Scope shape mirrors <see cref="GetTopForContextAsync"/>: same isUnrestricted/visibleRootIds
    /// contract, same GroupId-null-is-everyone-visible rule, same root-comparison against the group's Nested
    /// Set root (not a flattened subtree list) via the GroupId-to-Group join.
    /// </summary>
    /// <param name="isUnrestricted">True for an admin: every row is returned regardless of GroupId.</param>
    /// <param name="visibleRootIds">Ignored when <paramref name="isUnrestricted"/> is true. Otherwise a row is
    /// included when its GroupId is null, or its group's Nested Set root is in this set. An empty set fails
    /// closed to GroupId-null rows only - the same semantics AgentConditionVisibilityScope.Restricted with an
    /// empty set already carries for a planner with no GroupVisibility row.</param>
    /// <param name="take">Row cap applied after sorting, so the most urgent rows are the ones kept.</param>
    Task<List<AgentCondition>> GetOpenForScopeAsync(
        bool isUnrestricted,
        IReadOnlySet<Guid> visibleRootIds,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Total count of AgentConditionPlannerRelevantStatuses.Values rows a planner in this scope may see,
    /// ignoring <see cref="GetOpenForScopeAsync"/>'s take cap - so a caller can report "showing N of M" instead
    /// of silently truncating. Same scope contract as <see cref="GetOpenForScopeAsync"/>.
    /// </summary>
    Task<int> CountOpenForScopeAsync(
        bool isUnrestricted,
        IReadOnlySet<Guid> visibleRootIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a freshly detected condition together with its first audit event in one SaveChangesAsync,
    /// so a ledger row can never exist without the event that opened it. Returns null - not an exception -
    /// when the partial unique index on Fingerprint rejected the insert because another instance opened a
    /// row for the same fingerprint first; the caller is expected to re-read and treat it as known.
    /// </summary>
    Task<AgentCondition?> InsertAsync(AgentCondition condition, AgentConditionEvent detectionEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically moves a row from <paramref name="fromStatus"/> to <paramref name="toStatus"/> and, in the
    /// same database transaction, appends <paramref name="auditEvent"/>. Returns true only when this caller
    /// won the compare-and-swap. The transaction matters because the event rows are not just a log: the
    /// Etappe 5 action budget and circuit breaker are counted from them, so a transition without its event
    /// would silently not count against a safety limit. Because the method opens that transaction itself,
    /// it must not be called from inside IUnitOfWork.ExecuteInTransactionAsync or any other ambient
    /// transaction - EF refuses to nest one, and the call fails loudly rather than degrading.
    /// </summary>
    /// <remarks>
    /// False means "this caller did not win", which is ALMOST always "another instance moved the row first,
    /// nothing was written" - but not exclusively, so a caller must not read it as a guarantee that nothing
    /// happened. It is also false for an unknown or soft-deleted id, and, rarely, after the connection drops
    /// between a successful server-side commit and its acknowledgement: EnableRetryOnFailure (Program.cs)
    /// gives this context a retrying execution strategy, so the whole lambda is replayed, finds the row
    /// already past <paramref name="fromStatus"/>, and reports false although the first attempt did persist
    /// both the transition and its event. Etappe 5 must therefore treat a false as "skip this row for now",
    /// never as "this row's budget was not consumed".
    /// </remarks>
    Task<bool> TryTransitionAsync(
        Guid id,
        AgentConditionStatus fromStatus,
        AgentConditionStatus toStatus,
        AgentConditionTransitionFields? fields,
        AgentConditionEvent auditEvent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves LastSeenAtUtc forward on a still-open row. Guarded on the row being open and on the new value
    /// being strictly newer, so an out-of-order write can neither resurrect a terminal row's timestamp nor
    /// move the clock backwards. Returns whether a row was updated.
    /// </summary>
    Task<bool> TouchLastSeenAsync(Guid id, DateTime seenAtUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends a standalone audit event, for occurrences that are not status transitions (a failed
    /// remediation attempt that leaves Status unchanged, for example). Transitions carry their event
    /// through <see cref="TryTransitionAsync"/> instead, which is atomic with the status change.
    /// </summary>
    Task<AgentConditionEvent> InsertEventAsync(AgentConditionEvent conditionEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns up to <paramref name="take"/> planner-relevant open conditions (Detected, Reported,
    /// Prepared, Escalated - deliberately NOT AgentConditionStateMachine.OpenStatuses, which excludes
    /// Escalated, see that type's remarks) with High or Medium severity, for the per-turn context block
    /// (Etappe 3g). Never loads the full open set: severity, status and scope are filtered and the row
    /// count capped inside the database query itself, since this runs on every chat turn that carries a
    /// user id. <paramref name="isUnrestricted"/> true (Admin) skips the scope filter entirely; otherwise
    /// only rows with no GroupId, or whose group's Nested Set root is in <paramref name="visibleRootIds"/>,
    /// are eligible - the same subtree-via-root comparison PlanningAudienceResolver already uses for
    /// notification audience (Etappe 3e). Ranking: rows whose GroupId equals
    /// <paramref name="preferredGroupId"/> first (ranking only - never widens or narrows visibility), then
    /// Severity descending, then DetectedAtUtc ascending - the same priority order Etappe 5b specifies for
    /// the action dispatcher, reused here for consistency.
    /// </summary>
    Task<IReadOnlyList<AgentCondition>> GetTopForContextAsync(
        bool isUnrestricted,
        IReadOnlySet<Guid> visibleRootIds,
        Guid? preferredGroupId,
        int take,
        CancellationToken cancellationToken = default);
}
