// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when an employee contract expires within DaysUntilExpiry days and no follow-up
/// contract has been set up yet.
/// </summary>

using System.Globalization;
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
    public bool PlannersOnly => true;
    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.ContractExpiringSoon;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["name"] = ClientName,
        ["date"] = ValidUntil.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
        ["days"] = DaysUntilExpiry.ToString(CultureInfo.InvariantCulture)
    };

    public string DedupKey => ContractId.ToString();

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["contractId"] = ContractId,
        ["clientId"] = ClientId,
        ["clientName"] = ClientName,
        ["validUntil"] = ValidUntil,
        ["daysUntilExpiry"] = DaysUntilExpiry
    };
}
