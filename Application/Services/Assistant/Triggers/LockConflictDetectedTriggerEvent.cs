// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Trigger event fired when a wizard / harmonizer attempted to mutate a Work whose lock_level
/// disallows changes. The user should review the schedule before the next wizard run. GroupIds carries
/// every group the Work's Shift belongs to and narrows the audience to the planners who may see it; a
/// conflict whose work id could not be parsed out of the error text, or whose shift has no group
/// membership, reaches Admins only (RequiresGroupScope). The navigation hint in ActionParams can only
/// preselect one group and takes the first of the ordered set.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record LockConflictDetectedTriggerEvent(
    Guid WorkId,
    DateOnly Workday,
    int LockLevel,
    IReadOnlyCollection<Guid> GroupIds) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.LockConflict;
    public string Severity => LockLevel >= 2 ? AgentTriggerSeverity.High : AgentTriggerSeverity.Medium;
    public bool PlannersOnly => true;
    public bool RequiresGroupScope => true;
    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.LockConflict;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["date"] = Workday.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture)
    };

    public string DedupKey => $"{WorkId}:{Workday:yyyy-MM-dd}";

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
        ["workId"] = WorkId,
        ["workday"] = Workday,
        ["lockLevel"] = LockLevel,
        ["groupIds"] = GroupIds
    };
}
