// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when an active client lacks a core data field: MissingField is either AddressField
/// (no active address, medium severity) or ContactField (neither e-mail nor phone number,
/// low severity).
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record ClientMissingCoreDataTriggerEvent(
    Guid ClientId,
    string ClientName,
    string MissingField) : IAgentTriggerEvent
{
    public const string AddressField = "address";
    public const string ContactField = "contact";

    public string Kind => AgentTriggerKinds.ClientMissingCoreData;

    public string Severity => MissingField == AddressField
        ? AgentTriggerSeverity.Medium
        : AgentTriggerSeverity.Low;

    public bool PlannersOnly => true;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + (MissingField == AddressField
        ? ProactiveMessageI18nKeys.ClientMissingAddress
        : ProactiveMessageI18nKeys.ClientMissingContact);

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["name"] = ClientName
    };

    public string DedupKey => $"{ClientId}:{MissingField}";

    public string? ActionRoute => ProactiveActionRoutes.ClientEdit;

    public IReadOnlyDictionary<string, string>? ActionParams => new Dictionary<string, string>
    {
        [ProactiveActionParamKeys.ClientId] = ClientId.ToString()
    };

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["clientId"] = ClientId,
        ["clientName"] = ClientName,
        ["missingField"] = MissingField
    };
}
