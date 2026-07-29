// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when PlanStepExecutor pauses an AgentPlan at a step that requires human approval.
/// Without this event an unattended plan (e.g. one run by a background service overnight) would
/// only report the pause via SignalR, which nobody receives while offline, and the plan would
/// stay stuck in paused_for_approval forever. Targeted at the plan's owner (TargetUserId) and
/// dispatched through the existing proactive trigger pipeline so it also reaches the inbox for
/// offline users.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record PlanPausedForApprovalTriggerEvent(
    Guid PlanId,
    int StepIndex,
    int TotalSteps,
    string SkillName,
    Guid UserId) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.PlanPausedForApproval;

    public string Severity => AgentTriggerSeverity.Medium;

    public Guid? TargetUserId => UserId;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.PlanPausedForApproval;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["step"] = (StepIndex + 1).ToString(CultureInfo.InvariantCulture),
        ["total"] = TotalSteps.ToString(CultureInfo.InvariantCulture),
        ["skill"] = SkillName
    };

    public string DedupKey => PlanId.ToString() + ":" + StepIndex.ToString(CultureInfo.InvariantCulture);

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["planId"] = PlanId,
        ["stepIndex"] = StepIndex,
        ["totalSteps"] = TotalSteps,
        ["skillName"] = SkillName
    };
}
