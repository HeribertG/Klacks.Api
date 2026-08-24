// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// Append-only audit-history row for one AgentCondition: every state transition, attempt and
/// human decision against a condition-ledger row is recorded here so the row's full history survives
/// even though AgentCondition itself only holds the current state. EventType is a string rather than an
/// enum because the vocabulary is intentionally open: the closed lifecycle transitions
/// (Detected/Reported/Prepared/Executed/Rejected/Resolved/Escalated, mirroring AgentConditionStatus)
/// coexist with free-form operational events that are not state transitions at all (e.g. an
/// "AttemptFailed" retry that leaves Status unchanged). Etappe 3b (the ledger service) owns defining
/// and writing these values.
/// </summary>
public class AgentConditionEvent : BaseEntity
{
    public Guid ConditionId { get; set; }

    /// <summary>Free-form event/transition label; see this class's summary for the rationale.</summary>
    public string EventType { get; set; } = string.Empty;

    public DateTime AtUtc { get; set; }

    /// <summary>The human who caused this event, if any (null for detector- or system-driven events).</summary>
    public Guid? UserId { get; set; }

    public string? Detail { get; set; }
}
