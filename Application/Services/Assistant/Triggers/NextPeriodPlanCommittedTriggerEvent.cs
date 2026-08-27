// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when the FullyAutonomous branch has accepted the autofill scenario for a group's next
/// pay-period into the real schedule without human confirmation — possible only because the
/// scenario introduced zero new compliance issues. Tells the planners a live plan changed and
/// keeps the automatic acceptance auditable.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record NextPeriodPlanCommittedTriggerEvent(
    Guid GroupId,
    string GroupName,
    DateOnly PeriodStartDate,
    DateOnly PeriodEndDate,
    Guid ScenarioId) : IAgentTriggerEvent
{
    private const string CommittedDedupSuffix = ":committed";

    public string Kind => AgentTriggerKinds.NextPeriodSchedulingDue;

    public string Severity => AgentTriggerSeverity.Medium;

    public bool PlannersOnly => true;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.NextPeriodPlanCommitted;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["group"] = GroupName,
        ["date"] = PeriodStartDate.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture)
    };

    public string DedupKey => $"{GroupId}:{PeriodStartDate:yyyy-MM-dd}{CommittedDedupSuffix}";

    // Bridges the record's non-nullable GroupId to the interface's nullable member: a plain public
    // property of type Guid does not implicitly satisfy a Guid? interface member.
    Guid? IAgentTriggerEvent.GroupId => GroupId;

    public string? ActionRoute => ProactiveActionRoutes.Schedule;

    public IReadOnlyDictionary<string, string>? ActionParams => new Dictionary<string, string>
    {
        [ProactiveActionParamKeys.GroupId] = GroupId.ToString(),
        [ProactiveActionParamKeys.Date] = PeriodStartDate.ToString(ProactiveMessageFormats.ActionDate, CultureInfo.InvariantCulture)
    };

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["groupId"] = GroupId,
        ["groupName"] = GroupName,
        ["periodStartDate"] = PeriodStartDate,
        ["periodEndDate"] = PeriodEndDate,
        ["scenarioId"] = ScenarioId,
        ["autoCommitted"] = true
    };
}
