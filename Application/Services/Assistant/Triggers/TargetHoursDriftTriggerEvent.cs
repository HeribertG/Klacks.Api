// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when an employee's accumulated TargetHours deviation exceeds the configured threshold
/// (default ±12h). Disponent should rebalance the schedule for that employee.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record TargetHoursDriftTriggerEvent(
    Guid ClientId,
    string ClientName,
    decimal DriftHours,
    string PeriodLabel) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.TargetHoursDrift;
    public string Severity => Math.Abs(DriftHours) >= 24 ? AgentTriggerSeverity.High
        : Math.Abs(DriftHours) >= 12 ? AgentTriggerSeverity.Medium
        : AgentTriggerSeverity.Low;
    public string Summary => $"{ClientName} drifted {DriftHours:+0.0;-0.0;0} hours against target in {PeriodLabel}.";

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["clientId"] = ClientId,
        ["clientName"] = ClientName,
        ["driftHours"] = DriftHours,
        ["periodLabel"] = PeriodLabel
    };
}
