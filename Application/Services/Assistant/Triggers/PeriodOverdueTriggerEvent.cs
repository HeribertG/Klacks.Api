// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when a group's last completed pay period reached its close date DaysOverdue days ago and is
/// still not sealed. The close date is the period end plus LagDays; without a stored lag LagDays is 0 and
/// the close date is the period end. Severity rises to high once the close date lies
/// HighSeverityOverdueDays or more in the past.
/// </summary>
/// <param name="PeriodEndDate">Last day of the period; identifies the period and is where the action route opens</param>
/// <param name="DaysOverdue">Days from the close date to today</param>
/// <param name="LagDays">Days after the period end on which the period is closed, 0 when no lag is stored</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record PeriodOverdueTriggerEvent(
    Guid GroupId,
    string GroupName,
    DateOnly PeriodEndDate,
    int DaysOverdue,
    int LagDays = 0) : IAgentTriggerEvent
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
        ["days"] = (DaysOverdue + LagDays).ToString(CultureInfo.InvariantCulture)
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

    public IReadOnlyDictionary<string, object?> Payload
    {
        get
        {
            var payload = new Dictionary<string, object?>
            {
                ["groupId"] = GroupId,
                ["groupName"] = GroupName,
                ["periodEndDate"] = PeriodEndDate,
                ["daysOverdue"] = DaysOverdue
            };
            if (LagDays > 0)
            {
                payload["lagDays"] = LagDays;
            }

            return payload;
        }
    }
}
