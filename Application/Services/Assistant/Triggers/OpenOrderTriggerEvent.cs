// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when a Shift is still an unsealed OriginalOrder (ShiftStatus.OriginalOrder) whose FromDate
/// is today or later -- an ERP-imported or manually created order that has not yet been sealed into
/// a staffable shift. Unlike UnstaffedShiftTriggerEvent this never looks at staffing counts: an
/// order can be fully staffed and still be an open, unsealed draft. Severity escalates the closer
/// FromDate is. GroupIds carries every group the order's shift belongs to, which is what narrows the
/// audience to the planners who may see it; an order with no group membership at all reaches Admins
/// only (RequiresGroupScope).
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
    int DaysUntil,
    IReadOnlyCollection<Guid> GroupIds) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.OpenOrder;
    public string Severity => DaysUntil <= 7 ? AgentTriggerSeverity.High
        : DaysUntil <= 30 ? AgentTriggerSeverity.Medium
        : AgentTriggerSeverity.Low;
    public bool PlannersOnly => true;
    public bool RequiresGroupScope => true;
    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.OpenOrder;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["date"] = FromDate.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
        ["days"] = DaysUntil.ToString(CultureInfo.InvariantCulture)
    };

    public string DedupKey => DedupKeyFor(ShiftId, FromDate);

    public Guid? EntityId => ShiftId;

    /// <summary>
    /// The DedupKey spelling as a function of its key fields, so OpenOrderDetector's uncapped
    /// fingerprint scan can build the identical key from a key-only projection instead of restating
    /// the format.
    /// </summary>
    public static string DedupKeyFor(Guid shiftId, DateOnly fromDate) => $"{shiftId}:{fromDate:yyyy-MM-dd}";

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
        ["daysUntil"] = DaysUntil,
        ["groupIds"] = GroupIds
    };
}
