// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

/// <summary>
/// One entity's "Klacksy's remediation was applied here" marker for the service grid, read off the
/// condition ledger. Carries no display text: the grid renders the marker from TriggerKind and the
/// timestamp, and the ledger row's payload is deliberately not exposed on this path.
/// </summary>
public record ConditionAttributionDto
{
    /// <summary>The entity the condition was about - a container shift id for the service grid.</summary>
    public Guid EntityId { get; init; }

    /// <summary>
    /// When the row reached Executed. Nullable because the column is: every write path in this codebase
    /// stamps it through AgentConditionLedgerService, but a row that reached Executed some other way would
    /// otherwise be silently dropped from the attribution rather than shown without a time.
    /// </summary>
    public DateTime? HandledAtUtc { get; init; }

    /// <summary>The detector kind that found the condition, see AgentTriggerKinds.</summary>
    public string TriggerKind { get; init; } = string.Empty;
}
