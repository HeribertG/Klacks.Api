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
}
