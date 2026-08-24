// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when a Shift is still an unsealed OriginalOrder (ShiftStatus.OriginalOrder) whose FromDate
/// is today or later -- an ERP-imported or manually created order that has not yet been sealed into
/// a staffable shift. Unlike UnstaffedShiftTriggerEvent this never looks at staffing counts: an
/// order can be fully staffed and still be an open, unsealed draft. Severity escalates the closer
/// FromDate is.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record OpenOrderTriggerEvent(
    Guid ShiftId,
    Guid? ClientId,
    DateOnly FromDate,
    DateOnly? UntilDate,
    int DaysUntil) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.OpenOrder;
    public string Severity => DaysUntil <= 7 ? AgentTriggerSeverity.High
        : DaysUntil <= 30 ? AgentTriggerSeverity.Medium
        : AgentTriggerSeverity.Low;
    public bool PlannersOnly => true;
    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.OpenOrder;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["date"] = FromDate.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
        ["days"] = DaysUntil.ToString(CultureInfo.InvariantCulture)
    };

    public string DedupKey => $"{ShiftId}:{FromDate:yyyy-MM-dd}";

    public string? ActionRoute => ProactiveActionRoutes.Schedule;

    public IReadOnlyDictionary<string, string>? ActionParams => new Dictionary<string, string>
    {
        [ProactiveActionParamKeys.Date] = FromDate.ToString(ProactiveMessageFormats.ActionDate, CultureInfo.InvariantCulture)
    };

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["shiftId"] = ShiftId,
        ["clientId"] = ClientId,
        ["fromDate"] = FromDate,
        ["untilDate"] = UntilDate,
        ["daysUntil"] = DaysUntil
    };
}
