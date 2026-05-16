// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Phase 4 skeleton example trigger event: a shift slot N days from now is still unstaffed.
/// Constructed by a background scanner (TODO) and posted to IAgentTriggerService.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record UnstaffedShiftTriggerEvent(
    Guid ShiftId,
    DateOnly Workday,
    int DaysUntil,
    Guid? GroupId) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.UnstaffedShift;
    public string Severity => DaysUntil <= 3 ? AgentTriggerSeverity.High : DaysUntil <= 7 ? AgentTriggerSeverity.Medium : AgentTriggerSeverity.Low;
    public string Summary => $"Shift {ShiftId} on {Workday} ({DaysUntil} days from now) still unstaffed.";
    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["shiftId"] = ShiftId,
        ["workday"] = Workday,
        ["daysUntil"] = DaysUntil,
        ["groupId"] = GroupId
    };
}
