// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One escalation run asked to a roster in rank order until somebody acknowledges or the deadline passes.
/// Purpose says what the acknowledgement means: AbsenceCoverage ("which planner takes charge of this
/// shift?", the original flow that replaced AgentPlan as the carrier -
/// docs/ENTWURF-eskalationskette-2026-08-16.md §1 - because AgentPlan binds approve/abort to the user who
/// started the plan) or ProactiveApproval ("may Klacksy run this remediation?", keyed by ConditionId).
/// Both share the state machine, the conditional updates and the reply observer; only the identity
/// columns differ, which is why WorkId, the absence snapshot and ConditionId are all optional and the
/// partial unique indexes are per purpose key. ShiftStartUtc and DeadlineUtc are snapshots taken once
/// at chain start and never recomputed, so a shift edit or a later re-derivation of the roster cannot
/// shift a deadline a stage has already been notified against.
/// </summary>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant.Escalation;

public class EscalationChain : BaseEntity
{
    public EscalationChainStatus Status { get; set; } = EscalationChainStatus.Running;

    public EscalationChainPurpose Purpose { get; set; } = EscalationChainPurpose.AbsenceCoverage;

    /// <summary>The shift an AbsenceCoverage chain is about; null on a ProactiveApproval chain.</summary>
    public Guid? WorkId { get; set; }

    /// <summary>The agent_conditions row a ProactiveApproval chain guards; null on an AbsenceCoverage chain.</summary>
    public Guid? ConditionId { get; set; }

    /// <summary>Group the roster was resolved for; null for a proactive finding that has no group (admins only).</summary>
    public Guid? GroupId { get; set; }

    public DateTime? ShiftStartUtc { get; set; }

    public Guid? AbsentClientId { get; set; }

    public string AbsentClientName { get; set; } = string.Empty;

    /// <summary>
    /// The Break row BulkAddBreaksCommandHandler recorded for this absence, when the caller that
    /// started the chain could supply it. Optional: no dedicated "report" entity exists in this
    /// codebase (CoverAbsenceCommand takes raw facts, not a report id), so a chain started from
    /// elsewhere simply has no automatic Superseded path and falls back to a manual Cancelled.
    /// </summary>
    public Guid? AbsenceBreakId { get; set; }

    public DateTime DeadlineUtc { get; set; }

    public string? AcknowledgedByUserId { get; set; }

    public string? AcknowledgedByUserName { get; set; }

    public DateTime? AcknowledgedAtUtc { get; set; }

    public string? CancelledByUserId { get; set; }

    public string? CancelledByUserName { get; set; }

    public string? CancelReason { get; set; }

    public DateTime? CancelledAtUtc { get; set; }

    public string? OutcomeReason { get; set; }

    public virtual ICollection<EscalationStage> Stages { get; set; } = new List<EscalationStage>();
}
