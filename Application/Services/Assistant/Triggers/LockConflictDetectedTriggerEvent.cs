// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Trigger event fired when a wizard / harmonizer attempted to mutate a Work whose lock_level
/// disallows changes. The user should review the schedule before the next wizard run.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record LockConflictDetectedTriggerEvent(
    Guid WorkId,
    DateOnly Workday,
    int LockLevel,
    Guid? GroupId) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.LockConflict;
    public string Severity => LockLevel >= 2 ? AgentTriggerSeverity.High : AgentTriggerSeverity.Medium;
    public bool PlannersOnly => true;
    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.LockConflict;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["date"] = Workday.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture)
    };

    public string DedupKey => $"{WorkId}:{Workday:yyyy-MM-dd}";

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["workId"] = WorkId,
        ["workday"] = Workday,
        ["lockLevel"] = LockLevel,
        ["groupId"] = GroupId
    };
}
