// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when the FullyAutonomous branch produced an autofill scenario for a group's next
/// pay-period but withheld the automatic acceptance because the scenario introduces at least one
/// new compliance issue (or the accept gate refused it). The scenario is left as a draft, exactly
/// like the Autonomous branch, and this event asks a human to review it.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record NextPeriodAutoCommitBlockedTriggerEvent(
    Guid GroupId,
    string GroupName,
    DateOnly PeriodStartDate,
    DateOnly PeriodEndDate,
    Guid ScenarioId,
    int NewIssueCount) : IAgentTriggerEvent
{
    private const string CommitBlockedDedupSuffix = ":commit-blocked";

    public string Kind => AgentTriggerKinds.NextPeriodSchedulingDue;

    public string Severity => AgentTriggerSeverity.Medium;

    public bool PlannersOnly => true;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.NextPeriodAutoCommitBlocked;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["group"] = GroupName,
        ["date"] = PeriodStartDate.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
        ["issues"] = NewIssueCount.ToString(CultureInfo.InvariantCulture)
    };

    public string DedupKey => $"{GroupId}:{PeriodStartDate:yyyy-MM-dd}{CommitBlockedDedupSuffix}";

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
        ["newIssueCount"] = NewIssueCount,
        ["autoCommitted"] = false
    };
}
