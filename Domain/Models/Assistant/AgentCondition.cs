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
/// fingerprint every tick via LastSeenAtUtc while the condition persists; once a tick no longer reports
/// it, the ledger service marks the row Resolved. A condition detected again after Resolved is a
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

    /// <summary>Updated on every tick the detector still reports this fingerprint; drives Resolved detection.</summary>
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
    /// Set when this row was itself caused by an earlier Klacksy remediation (Etappe 5 cascade guard:
    /// such rows are never auto-handled again, only hinted).
    /// </summary>
    public Guid? CausedByConditionId { get; set; }

    /// <summary>
    /// Single-condition override of the kind's governance MaxAction (Etappe 4 delegation). Left as a
    /// plain int until Etappe 4 introduces ProactiveMaxAction (Hint/Prepare/Execute) - no migration
    /// needed to retarget the CLR type onto that enum later, the stored column stays integer.
    /// </summary>
    public int? DelegatedMaxAction { get; set; }

    public Guid? DelegatedByUserId { get; set; }

    /// <summary>Structured extra data the detector captured about this condition (free-form JSON).</summary>
    public string PayloadJson { get; set; } = "{}";
}
