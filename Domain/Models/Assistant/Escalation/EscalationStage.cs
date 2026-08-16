// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One person's turn in an EscalationChain, frozen from the roster at chain start so a later
/// re-derivation of the roster cannot reorder a chain that is already running. DueAtUtc is the field
/// PlanStep never had: a rolling deadline lives on the stage itself, not on a shared counter, so two
/// concurrent sweeps can race for the same stage and only one wins (see EscalationChain's XML doc).
/// UserId is a string without a foreign key on purpose: AppUser.Id is text, and AppUser is the one
/// entity without soft delete, so a name snapshot (mirroring PeriodAuditLog.PerformedByName) is the
/// only way this stage stays readable after the user is hard-deleted.
/// </summary>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant.Escalation;

public class EscalationStage : BaseEntity
{
    public Guid EscalationChainId { get; set; }

    public virtual EscalationChain? Chain { get; set; }

    public int Rank { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string UserDisplayName { get; set; } = string.Empty;

    public EscalationStageStatus Status { get; set; } = EscalationStageStatus.Pending;

    public string? SkipReason { get; set; }

    public DateTime? NotifiedAtUtc { get; set; }

    public DateTime? DueAtUtc { get; set; }

    public DateTime? RespondedAtUtc { get; set; }

    public string? DeliveryChannel { get; set; }

    public string? DeliveryOutcome { get; set; }

    /// <summary>
    /// The inbox row this stage's notification produced, if any. Lets a later report join a stage to
    /// its ReadAtUtc/Reaction without duplicating that state here.
    /// </summary>
    public Guid? DispatchRowId { get; set; }
}
