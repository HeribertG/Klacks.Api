// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

public class UnresolvedEscalationChainResource
{
    public Guid Id { get; set; }
    public string Purpose { get; set; } = string.Empty;
    /// <summary>Cancelled or Exhausted.</summary>
    public string Status { get; set; } = string.Empty;
    public string AbsentClientName { get; set; } = string.Empty;
    public DateTime? ShiftStartUtc { get; set; }
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public string? FindingKind { get; set; }
    /// <summary>When the chain was cancelled or lapsed.</summary>
    public DateTime? EndedAtUtc { get; set; }
    public string? CancelledByUserName { get; set; }
    /// <summary>The cancel reason, only for an administrator, a supervisor or whoever cancelled; null for everybody else.</summary>
    public string? CancelReason { get; set; }
    /// <summary>True when a reason exists but is withheld from the caller.</summary>
    public bool CancelReasonHidden { get; set; }
    /// <summary>How many planners were actually woken before the chain ended.</summary>
    public int NotifiedStageCount { get; set; }
    /// <summary>Status of the prepared recovery scenario (Active, Accepted, Rejected, Superseded); null when there is none or it was deleted.</summary>
    public string? ScenarioStatus { get; set; }
    /// <summary>The scenario id while it can still be opened (Active), otherwise null.</summary>
    public Guid? ScenarioId { get; set; }
}
