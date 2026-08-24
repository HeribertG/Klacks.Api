// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The condition ledger's write side: turns a detector's per-tick observations into user-independent
/// memory rows and guards every status change against the lifecycle state machine. Fingerprint creation
/// is deliberately NOT part of this service - the detector owns it, because only the detector knows which
/// business date and entity identify its finding. The ledger stores the fingerprint verbatim and treats it
/// as the row's identity.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentConditionLedgerService
{
    /// <summary>
    /// Records that a detector saw this condition in the current tick. An already open row for the
    /// fingerprint only gets its LastSeenAtUtc moved forward and comes back with IsNew false; otherwise a
    /// new Detected row is opened and comes back with IsNew true. A fingerprint whose previous row is
    /// Resolved therefore re-arms into a brand-new row and leaves the resolved one intact as history -
    /// no special case, that is simply what the partial unique index permits.
    /// </summary>
    Task<(AgentCondition Condition, bool IsNew)> UpsertDetectedAsync(
        string triggerKind,
        string fingerprint,
        Guid? entityId,
        Guid? groupId,
        string severity,
        string payloadJson,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes every open row of this kind whose fingerprint is absent from the current tick's observation,
    /// and returns how many were closed.
    /// </summary>
    /// <param name="triggerKind">The detector kind whose open rows are reconciled.</param>
    /// <param name="completeFingerprintSet">
    /// PRECONDITION: the COMPLETE set of fingerprints the kind currently holds - not a capped page of it.
    /// Absence from this set is read as "the condition is gone", so a detector that limits its scan
    /// (MaxFindingsPerTick, Take(n), a time window) must not call this: everything beyond its cap would be
    /// resolved every tick and re-armed as a new row the next one, which destroys the ledger's memory
    /// instead of maintaining it. Call once per kind per tick, from the tick, not per detector finding.
    /// </param>
    Task<int> MarkResolvedAsync(
        string triggerKind,
        IReadOnlySet<string> completeFingerprintSet,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves one row along the lifecycle and appends the matching audit event atomically. Returns false
    /// only when the compare-and-swap was lost, i.e. another instance already moved the row - callers may
    /// treat that as "somebody else has it" and skip. A transition that is not part of the state machine
    /// is a programming error and throws instead, so a caller that skips on false cannot silently swallow
    /// one. The timestamps that follow from the target status (ResolvedAtUtc, EscalatedAtUtc, HandledAtUtc
    /// for Executed and Rejected) are filled in here; anything else the transition should write comes in
    /// through <paramref name="fields"/>.
    /// </summary>
    Task<bool> TryTransitionAsync(
        Guid conditionId,
        AgentConditionStatus fromStatus,
        AgentConditionStatus toStatus,
        Guid? userId = null,
        string? detail = null,
        AgentConditionTransitionFields? fields = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a human's rejection of this finding onto the ledger row: reads the status the row is
    /// actually in and moves it from exactly there to Rejected, recording the reason and the rejecting
    /// user. Deliberately total rather than throwing - unlike <see cref="TryTransitionAsync"/> this is
    /// driven by a user action whose primary effect (the dismissal on the notification itself) has
    /// already been persisted by the caller, so every way of not reaching Rejected is a false, never an
    /// exception: an unknown id, a row already Executed, Resolved, Escalated or still Detected (the
    /// state machine grants Rejected only from Reported and Prepared), and a lost compare-and-swap.
    /// A false therefore means "the finding was not marked rejected", never "the dismissal failed".
    ///
    /// FIRST REJECTER WINS, and the others are not recorded. One finding is reported to every planner
    /// in its audience, so several people hold their own notification of the same ledger row. The first
    /// to dismiss moves the row to Rejected and stamps their reason; every later dismissal of the same
    /// row finds it terminal and returns false, so that person's reason is lost - their own dismissal
    /// is still stored on their notification, and they are told nothing. That is deliberate for the
    /// row's lifecycle (the ledger holds world state, not one opinion per user) but it means
    /// RejectReason is a sample of one, not a consensus. A stage that learns from these reasons
    /// (Etappe 6) must either accept that sample or collect reasons per user somewhere else first.
    /// </summary>
    Task<bool> TryRejectAsync(
        Guid conditionId,
        AgentConditionRejectReason rejectReason,
        Guid? rejectedByUserId,
        CancellationToken cancellationToken = default);
}
