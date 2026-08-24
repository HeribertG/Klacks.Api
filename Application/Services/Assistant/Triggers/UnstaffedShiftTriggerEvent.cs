// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A shift slot N days from now is still unstaffed. Emitted by UnstaffedShift7dDetector and posted to
/// IAgentTriggerService. GroupIds carries every group the shift belongs to and narrows the audience to
/// the planners who may see it; a shift with no group membership at all — the detector scans with
/// ShowUngroupedShifts on, so these do occur — reaches Admins only (RequiresGroupScope). The
/// navigation hint in ActionParams can only preselect one group and takes the first of the ordered set.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record UnstaffedShiftTriggerEvent(
    Guid ShiftId,
    DateOnly Workday,
    int DaysUntil,
    IReadOnlyCollection<Guid> GroupIds) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.UnstaffedShift;
    public string Severity => DaysUntil <= 3 ? AgentTriggerSeverity.High
        : DaysUntil <= 7 ? AgentTriggerSeverity.Medium
        : AgentTriggerSeverity.Low;
    public bool PlannersOnly => true;
    public bool RequiresGroupScope => true;
    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.UnstaffedShift;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["date"] = Workday.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
        ["days"] = DaysUntil.ToString(CultureInfo.InvariantCulture)
    };

    public string DedupKey => DedupKeyFor(ShiftId, Workday);

    public Guid? EntityId => ShiftId;

    /// <summary>
    /// The DedupKey spelling as a function of its key fields, so UnstaffedShift7dDetector's uncapped
    /// fingerprint scan can build the identical key without restating the format.
    /// </summary>
    public static string DedupKeyFor(Guid shiftId, DateOnly workday) => $"{shiftId}:{workday:yyyy-MM-dd}";

    public string? ActionRoute => ProactiveActionRoutes.Schedule;

    public IReadOnlyDictionary<string, string>? ActionParams
    {
        get
        {
            var actionParams = new Dictionary<string, string>
            {
                [ProactiveActionParamKeys.Date] = Workday.ToString(ProactiveMessageFormats.ActionDate, CultureInfo.InvariantCulture)
            };
            if (GroupIds.Count > 0)
            {
                actionParams[ProactiveActionParamKeys.GroupId] = GroupIds.First().ToString();
            }

            return actionParams;
        }
    }

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["shiftId"] = ShiftId,
        ["workday"] = Workday,
        ["daysUntil"] = DaysUntil,
        ["groupIds"] = GroupIds
    };
}
