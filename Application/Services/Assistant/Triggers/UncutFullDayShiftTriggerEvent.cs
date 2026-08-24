// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when a Shift is still Status == OriginalShift (never cut into day/night pieces) with
/// StartShift == EndShift, the FullDay convention TimeRange.ForWorkingTime assigns equal bounds:
/// a 24 hour duty rather than a zero-length span. DaysUntil is negative once FromDate has passed,
/// which the Severity mapping below treats as the most urgent case (an active, unstaffed-by-shift-
/// pattern duty), not as stale data to ignore.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record UncutFullDayShiftTriggerEvent(
    Guid ShiftId,
    DateOnly FromDate,
    int DaysUntil,
    Guid? GroupId) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.UncutFulldayShift;
    public string Severity => DaysUntil <= 7 ? AgentTriggerSeverity.High
        : DaysUntil <= 30 ? AgentTriggerSeverity.Medium
        : AgentTriggerSeverity.Low;
    public bool PlannersOnly => true;
    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.UncutFulldayShift;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["date"] = FromDate.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
        ["days"] = DaysUntil.ToString(CultureInfo.InvariantCulture)
    };

    public string DedupKey => DedupKeyFor(ShiftId);

    public Guid? EntityId => ShiftId;

    /// <summary>
    /// The DedupKey spelling as a function of its key field, so UncutFullDayShiftDetector's uncapped
    /// fingerprint scan can build the identical key from a key-only projection instead of restating
    /// the format.
    /// </summary>
    public static string DedupKeyFor(Guid shiftId) => shiftId.ToString();

    public string? ActionRoute => ProactiveActionRoutes.Schedule;

    public IReadOnlyDictionary<string, string>? ActionParams
    {
        get
        {
            var actionParams = new Dictionary<string, string>
            {
                [ProactiveActionParamKeys.Date] = FromDate.ToString(ProactiveMessageFormats.ActionDate, CultureInfo.InvariantCulture)
            };
            if (GroupId is Guid groupId)
            {
                actionParams[ProactiveActionParamKeys.GroupId] = groupId.ToString();
            }

            return actionParams;
        }
    }

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["shiftId"] = ShiftId,
        ["fromDate"] = FromDate,
        ["daysUntil"] = DaysUntil,
        ["groupId"] = GroupId
    };
}
