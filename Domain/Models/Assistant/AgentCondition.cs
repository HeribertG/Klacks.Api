// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// User-independent memory row for a condition a proactive detector observed (Etappe 3 of the
/// Klacksy-proactive plan). Tracks the finding's lifecycle from first detection through reporting,
/// remediation and resolution, independent of any single user's notification state. Fingerprint is
/// deterministic from TriggerKind + EntityId + the relevant business date - never from display text -
/// so the same underlying condition always maps to the same open row. A detector re-reports the same
/// fingerprint every tick while the condition persists, which moves LastSeenAtUtc forward and refreshes
/// PayloadJson when the observation changed; once a tick no longer reports it, the ledger service marks
/// the row Resolved. Nothing else about an open row is ever rewritten by a re-observation - status,
/// attempt counters and handling stamps belong to the row's own lifecycle, not to the detector's view of
/// the world. A condition detected again after Resolved is a
/// re-arm: it inserts a new row with the same fingerprint rather than reopening the old one, which is
/// what the partial unique index on Fingerprint (open statuses only) allows.
/// </summary>
public class AgentCondition : BaseEntity
{
    /// <summary>Detector kind constant, see AgentTriggerKinds.</summary>
    public string TriggerKind { get; set; } = string.Empty;

    /// <summary>Deterministic identity of the underlying condition, e.g. "{TriggerKind}:{EntityId}:{RelevantDate:yyyy-MM-dd}".</summary>
    public string Fingerprint { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    public Guid? GroupId { get; set; }

    /// <summary>See AgentTriggerSeverity (High/Medium/Low) - reused from the existing trigger pipeline.</summary>
    public string Severity { get; set; } = string.Empty;

    public AgentConditionStatus Status { get; set; } = AgentConditionStatus.Detected;

    public DateTime DetectedAtUtc { get; set; }

    /// <summary>Moved forward on every tick the detector still reports this fingerprint; drives Resolved detection.</summary>
    public DateTime LastSeenAtUtc { get; set; }

    public DateTime? ResolvedAtUtc { get; set; }

    public DateTime? HandledAtUtc { get; set; }

    public AgentConditionHandlingKind HandlingKind { get; set; } = AgentConditionHandlingKind.None;

    /// <summary>The AnalyseScenario prepared as this condition's remediation, once HandlingKind reaches ScenarioPrepared.</summary>
    public Guid? ScenarioId { get; set; }

    /// <summary>Number of remediation attempts made against this row (drives the Etappe 5 Escalated-after-3 rule).</summary>
    public int AttemptCount { get; set; }

    public DateTime? LastAttemptAtUtc { get; set; }

    public DateTime? EscalatedAtUtc { get; set; }

    public AgentConditionRejectReason? RejectReason { get; set; }

    public Guid? RejectedByUserId { get; set; }

    /// <summary>
    /// The human who released Klacksy's prepared remediation, stamped when accepting its scenario moved
    /// this row to Executed. It exists because the row otherwise records only that a remediation happened,
    /// never on whose authority: CurrentUserUpdated is BaseEntity audit noise that the next write to the
    /// row overwrites, and RejectedByUserId is by definition the opposite decision. Null on every row that
    /// reached Executed through the autonomous action dispatcher, which is the honest answer - nobody
    /// approved those - and on every row still open.
    /// </summary>
    public Guid? ApprovedByUserId { get; set; }

    /// <summary>
    /// Set when this row was itself caused by an earlier Klacksy remediation (Etappe 5 cascade guard:
    /// such rows are never auto-handled again, only hinted).
    /// </summary>
    public Guid? CausedByConditionId { get; set; }

    /// <summary>
    /// Single-condition override of the kind's governance MaxAction (Etappe 4e delegation, "mach du").
    /// Retargeted from plain int onto ProactiveMaxAction now that Etappe 4a introduced it - no migration
    /// needed, EF maps an unconverted enum to the same integer column a plain int would. Never widens
    /// what the kind's governance allows by itself; the delegating user's own rights against this value
    /// are enforced in DelegateConditionCommandHandler, before this column is ever written.
    /// </summary>
    public ProactiveMaxAction? DelegatedMaxAction { get; set; }

    public Guid? DelegatedByUserId { get; set; }

    /// <summary>
    /// Structured extra data the detector captured about this condition (free-form JSON). Refreshed on
    /// re-observation while the row is open, so a payload shape introduced after the row was opened still
    /// reaches it; a detector reporting nothing structured leaves the stored value alone rather than
    /// clearing it.
    /// </summary>
    public string PayloadJson { get; set; } = "{}";
}
