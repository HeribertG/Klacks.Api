// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One outage-escalation run: "which planner takes charge of this shift?", asked to the roster of a
/// group in rank order until somebody acknowledges or the deadline passes. Replaces AgentPlan as the
/// carrier for this flow (docs/ENTWURF-eskalationskette-2026-08-16.md §1) because AgentPlan binds
/// approve/abort to the same user who started the plan, which is the opposite of what an escalation
/// needs. ShiftStartUtc and DeadlineUtc are snapshots taken once at chain start and never
/// recomputed, so a shift edit or a later re-derivation of the roster cannot shift a deadline a
/// stage has already been notified against.
/// </summary>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant.Escalation;

public class EscalationChain : BaseEntity
{
    public EscalationChainStatus Status { get; set; } = EscalationChainStatus.Running;

    public Guid WorkId { get; set; }

    public Guid GroupId { get; set; }

    public DateTime ShiftStartUtc { get; set; }

    public Guid AbsentClientId { get; set; }

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
