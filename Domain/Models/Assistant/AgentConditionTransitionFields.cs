// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Optional column values written together with a condition-ledger status transition, in the same
/// conditional update that performs the compare-and-swap. Replaces the mutate-the-entity callback a
/// tracked update would use, because the transition runs as a single UPDATE ... WHERE status = expected
/// and never materialises the row. Every field is apply-if-set: a null leaves the stored value untouched
/// (the update writes COALESCE(@value, column)), so there is deliberately no way to clear a column back
/// to NULL through this record - a future stage that needs clearing has to add an explicit flag for it.
///
/// <see cref="AttemptIncrement"/> is the one exception to apply-if-set, and has to be: an attempt
/// counter is a read-modify-write, and a value passed in from outside would be computed from a row read
/// before the swap - the very race the compare-and-swap exists to prevent. It is therefore applied as a
/// relative "+ n" inside the same UPDATE, with 0 meaning "leave it alone".
/// </summary>
/// <param name="ResolvedAtUtc">Set by the ledger service itself on a transition to Resolved.</param>
/// <param name="HandledAtUtc">Set by the ledger service itself on a transition to Executed or Rejected; a caller may supply it for Prepared.</param>
/// <param name="EscalatedAtUtc">Set by the ledger service itself on a transition to Escalated.</param>
/// <param name="ScenarioId">The AnalyseScenario prepared as this condition's remediation (Etappe 4).</param>
/// <param name="HandlingKind">How the condition was handled, paired with HandledAtUtc (Etappe 4/5).</param>
/// <param name="RejectReason">Structured reason a human rejected the finding or its remediation.</param>
/// <param name="RejectedByUserId">The human who rejected it.</param>
/// <param name="LastAttemptAtUtc">When the remediation attempt this transition claims was started (Etappe 5b); also what the stale-claim window is measured from.</param>
/// <param name="AttemptIncrement">Added to AttemptCount inside the same UPDATE. Raised on the CLAIM, never on the outcome, so a run that dies mid-remediation still counts.</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record AgentConditionTransitionFields(
    DateTime? ResolvedAtUtc = null,
    DateTime? HandledAtUtc = null,
    DateTime? EscalatedAtUtc = null,
    Guid? ScenarioId = null,
    AgentConditionHandlingKind? HandlingKind = null,
    AgentConditionRejectReason? RejectReason = null,
    Guid? RejectedByUserId = null,
    DateTime? LastAttemptAtUtc = null,
    int AttemptIncrement = 0);
