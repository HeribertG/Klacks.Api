// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Phase 4 skeleton example trigger event: a shift slot N days from now is still unstaffed.
/// Constructed by a background scanner (TODO) and posted to IAgentTriggerService.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record UnstaffedShiftTriggerEvent(
    Guid ShiftId,
    DateOnly Workday,
    int DaysUntil,
    Guid? GroupId) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.UnstaffedShift;
    public string Severity => DaysUntil <= 3 ? AgentTriggerSeverity.High
        : DaysUntil <= 7 ? AgentTriggerSeverity.Medium
        : AgentTriggerSeverity.Low;
    public bool PlannersOnly => true;
    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.UnstaffedShift;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["date"] = Workday.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
        ["days"] = DaysUntil.ToString(CultureInfo.InvariantCulture)
    };

    public string DedupKey => $"{ShiftId}:{Workday:yyyy-MM-dd}";

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["shiftId"] = ShiftId,
        ["workday"] = Workday,
        ["daysUntil"] = DaysUntil,
        ["groupId"] = GroupId
    };
}
