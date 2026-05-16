// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when an employee contract expires within DaysUntilExpiry days and no follow-up
/// contract has been set up yet.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record ContractExpiringSoonTriggerEvent(
    Guid ContractId,
    Guid ClientId,
    string ClientName,
    DateOnly ValidUntil,
    int DaysUntilExpiry) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.ContractExpiringSoon;
    public string Severity => DaysUntilExpiry <= 7 ? AgentTriggerSeverity.High
        : DaysUntilExpiry <= 30 ? AgentTriggerSeverity.Medium
        : AgentTriggerSeverity.Low;
    public string Summary => $"{ClientName}'s contract expires {ValidUntil} (in {DaysUntilExpiry} day(s)) with no follow-up.";

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["contractId"] = ContractId,
        ["clientId"] = ClientId,
        ["clientName"] = ClientName,
        ["validUntil"] = ValidUntil,
        ["daysUntilExpiry"] = DaysUntilExpiry
    };
}
