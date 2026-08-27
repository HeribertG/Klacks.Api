// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when a group's next pay-period starts within the configured lead window and no schedule
/// draft (AnalyseScenario) covering that period exists yet — the hint shape emitted when the
/// effective autonomy level does not allow starting the AutoWizard automatically, or when an
/// automatic start was not possible.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record NextPeriodSchedulingDueTriggerEvent(
    Guid GroupId,
    string GroupName,
    DateOnly PeriodStartDate,
    DateOnly PeriodEndDate,
    int DaysUntilStart) : IAgentTriggerEvent
{
    private const int HighUrgencyDays = 2;

    public string Kind => AgentTriggerKinds.NextPeriodSchedulingDue;

    public string Severity => DaysUntilStart <= HighUrgencyDays
        ? AgentTriggerSeverity.High
        : AgentTriggerSeverity.Medium;

    public bool PlannersOnly => true;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.NextPeriodSchedulingDue;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["group"] = GroupName,
        ["date"] = PeriodStartDate.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
        ["days"] = DaysUntilStart.ToString(CultureInfo.InvariantCulture)
    };

    public string DedupKey => $"{GroupId}:{PeriodStartDate:yyyy-MM-dd}";

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
        ["daysUntilStart"] = DaysUntilStart
    };
}
