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
    /// fingerprint gets its LastSeenAtUtc moved forward and comes back with IsNew false; otherwise a
    /// new Detected row is opened and comes back with IsNew true. A fingerprint whose previous row is
    /// Resolved therefore re-arms into a brand-new row and leaves the resolved one intact as history -
    /// no special case, that is simply what the partial unique index permits.
    ///
    /// PAYLOAD IS NO LONGER WRITE-ONCE (2026-08-26). Until this change a re-observation moved only
    /// LastSeenAtUtc, so PayloadJson was frozen at the moment the row was opened. That made the ledger's
    /// memory actively harmful rather than merely incomplete: a detector that started capturing a new
    /// field, or a remediation binder that started requiring one, could never reach a row that was already
    /// open - and since an open row is only closed by resolution, rejection or a successful remediation the
    /// payload itself has to enable, the backlog was permanently unremediable. A re-observation now also
    /// REWRITES PayloadJson when the detector reports something different from what is stored.
    ///
    /// What deliberately does NOT change, so the row stays the same memory it was: the fingerprint (a
    /// changed fingerprint would open a second row and split the history), Status, AttemptCount,
    /// HandledAtUtc, RejectReason, DetectedAtUtc and every other column. Only rows the state machine still
    /// counts as open are reachable here at all - a terminal row is history and is never returned by the
    /// fingerprint lookup this method builds on.
    ///
    /// The write is skipped when the stored payload already equals the reported one, because a tick
    /// re-observes every open row and almost none of them have changed.
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

    /// <summary>
    /// Resumes an abandoned remediation claim (Etappe 5b): a Prepared row whose LastAttemptAtUtc is
    /// older than <paramref name="staleAfter"/> is claimed again, its AttemptCount raised and a
    /// Reclaimed audit event appended atomically. False means the row is not resumable - it is not
    /// Prepared, its claim is still fresh, or another instance took it first - and is never an error.
    /// </summary>
    /// <param name="conditionId">The Prepared row to resume.</param>
    /// <param name="staleAfter">Age a claim has to exceed before it counts as abandoned.</param>
    /// <param name="detail">Audit detail; must carry the action-claim marker to count against budget.</param>
    Task<bool> TryReclaimStaleAsync(
        Guid conditionId,
        TimeSpan staleAfter,
        string detail,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends an audit event that is NOT a status transition - a failed remediation attempt above all,
    /// where the row deliberately stays Prepared so the stale-claim path can retry it.
    /// </summary>
    Task RecordEventAsync(
        Guid conditionId,
        string eventType,
        string detail,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks this row as caused by an earlier Klacksy remediation (Etappe 5b cascade guard), so it is
    /// never auto-handled again and the provenance survives in the ledger. First attribution wins;
    /// false means the row already carried one or does not exist.
    /// </summary>
    Task<bool> TrySetCausedByAsync(
        Guid conditionId,
        Guid causedByConditionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a human's one-off "handle this yourself" grant for a single condition row (Etappe 4e).
    /// Only ever narrows what MaxAction the row may reach beyond the kind's own governance for exactly
    /// this row - it never widens governance, and it never touches Status. Returns false when the row
    /// does not exist or is no longer AgentConditionPlannerRelevantStatuses.Values: delegating a
    /// resolved, rejected or executed finding has nothing left to act on. Whether
    /// <paramref name="delegatingUserId"/> is even allowed to request <paramref name="maxAction"/> - both
    /// their role tier and whether this condition is within their own group-visibility scope - is the
    /// caller's responsibility (DelegateConditionCommandHandler); by the time this runs, the grant is
    /// already authorised.
    /// </summary>
    Task<bool> TryDelegateAsync(
        Guid conditionId,
        ProactiveMaxAction maxAction,
        Guid delegatingUserId,
        CancellationToken cancellationToken = default);
}
