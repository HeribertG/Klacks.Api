// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when a pay-period's close date is within DaysUntilDue days and the period
/// is still open.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record PeriodCloseDueTriggerEvent(
    Guid GroupId,
    string GroupName,
    DateOnly PeriodEndDate,
    int DaysUntilDue) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.PeriodCloseDue;
    public string Severity => DaysUntilDue <= 1 ? AgentTriggerSeverity.High
        : DaysUntilDue <= 3 ? AgentTriggerSeverity.Medium
        : AgentTriggerSeverity.Low;
    public bool PlannersOnly => true;
    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.PeriodCloseDue;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["group"] = GroupName,
        ["date"] = PeriodEndDate.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
        ["days"] = DaysUntilDue.ToString(CultureInfo.InvariantCulture)
    };

    public string DedupKey => $"{GroupId}:{PeriodEndDate:yyyy-MM-dd}";

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["groupId"] = GroupId,
        ["groupName"] = GroupName,
        ["periodEndDate"] = PeriodEndDate,
        ["daysUntilDue"] = DaysUntilDue
    };
}
