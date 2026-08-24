// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when a group's last completed pay period ended DaysOverdue days ago and is still
/// not sealed. Severity rises to high once the period end lies HighSeverityOverdueDays
/// or more in the past.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record PeriodOverdueTriggerEvent(
    Guid GroupId,
    string GroupName,
    DateOnly PeriodEndDate,
    int DaysOverdue) : IAgentTriggerEvent
{
    private const int HighSeverityOverdueDays = 21;

    public string Kind => AgentTriggerKinds.PeriodOverdue;

    public string Severity => DaysOverdue >= HighSeverityOverdueDays
        ? AgentTriggerSeverity.High
        : AgentTriggerSeverity.Medium;

    public bool PlannersOnly => true;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.PeriodOverdue;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["group"] = GroupName,
        ["periodEnd"] = PeriodEndDate.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
        ["days"] = DaysOverdue.ToString(CultureInfo.InvariantCulture)
    };

    public string DedupKey => $"{GroupId}:{PeriodEndDate:yyyy-MM-dd}";

    // Bridges the record's non-nullable GroupId to the interface's nullable member: a plain public
    // property of type Guid does not implicitly satisfy a Guid? interface member.
    Guid? IAgentTriggerEvent.GroupId => GroupId;

    public string? ActionRoute => ProactiveActionRoutes.PeriodClosing;

    public IReadOnlyDictionary<string, string>? ActionParams => new Dictionary<string, string>
    {
        [ProactiveActionParamKeys.GroupId] = GroupId.ToString(),
        [ProactiveActionParamKeys.Date] = PeriodEndDate.ToString(ProactiveMessageFormats.ActionDate, CultureInfo.InvariantCulture)
    };

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["groupId"] = GroupId,
        ["groupName"] = GroupName,
        ["periodEndDate"] = PeriodEndDate,
        ["daysOverdue"] = DaysOverdue
    };
}
