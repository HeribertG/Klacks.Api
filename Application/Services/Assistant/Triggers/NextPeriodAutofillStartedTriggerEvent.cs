// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when the detector has started the AutoWizard chain for a group's next pay-period on its
/// own, because the effective autonomy level permits it. Informative only — the chain produces a
/// draft scenario that a human must still review and accept; this event makes the automatic start
/// visible and auditable to the planners.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record NextPeriodAutofillStartedTriggerEvent(
    Guid GroupId,
    string GroupName,
    DateOnly PeriodStartDate,
    DateOnly PeriodEndDate,
    Guid JobId,
    bool AutoCommitIntended) : IAgentTriggerEvent
{
    private const string AutofillDedupSuffix = ":autofill";

    public string Kind => AgentTriggerKinds.NextPeriodSchedulingDue;

    public string Severity => AgentTriggerSeverity.Low;

    public bool PlannersOnly => true;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.NextPeriodAutofillStarted;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["group"] = GroupName,
        ["date"] = PeriodStartDate.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture)
    };

    public string DedupKey => $"{GroupId}:{PeriodStartDate:yyyy-MM-dd}{AutofillDedupSuffix}";

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
        ["jobId"] = JobId,
        ["autoCommitIntended"] = AutoCommitIntended
    };
}
