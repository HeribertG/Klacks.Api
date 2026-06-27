// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when an AnalyseScenario has been waiting for accept / reject longer than 48 hours.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record ScenarioPendingTriggerEvent(
    Guid ScenarioId,
    int HoursPending,
    Guid? GroupId,
    string GroupName) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.ScenarioPending;
    public string Severity => HoursPending >= 168 ? AgentTriggerSeverity.High
        : HoursPending >= 72 ? AgentTriggerSeverity.Medium
        : AgentTriggerSeverity.Low;
    public bool PlannersOnly => true;
    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.ScenarioPending;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["group"] = GroupName,
        ["hours"] = HoursPending.ToString(CultureInfo.InvariantCulture)
    };

    public string DedupKey => ScenarioId.ToString();

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["scenarioId"] = ScenarioId,
        ["hoursPending"] = HoursPending,
        ["groupId"] = GroupId,
        ["groupName"] = GroupName
    };
}
