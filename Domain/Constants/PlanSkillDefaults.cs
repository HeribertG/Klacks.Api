// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared constants for the chat-facing planning skills (create_plan / get_plan_status) and the
/// deterministic plan-trigger nudge. Holds the skill names, the parameter keys used to carry a
/// proposed plan through the confirmation replay, the override flag that distinguishes the
/// proposal call from the confirmed execution call, and the system-prompt nudge text.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class PlanSkillDefaults
{
    public const string CreatePlanSkillName = "create_plan";

    public const string GetPlanStatusSkillName = "get_plan_status";

    public const string GoalParameter = "goal";

    public const string PlanIdParameter = "planId";

    public const string ExecuteConfirmedParameter = "planExecutionConfirmed";

    public const string ExecuteConfirmedValue = "true";

    public const int MinMutationTargets = 2;

    public const string PlanNudgeNote =
        "This message asks for several state-changing actions at once and no guided recipe covers it. " +
        "Prefer calling the 'create_plan' tool with a concise free-text 'goal' that captures the whole " +
        "request, instead of executing the individual actions one by one. create_plan only PROPOSES the " +
        "steps and asks the user to confirm; it never executes on its own.";
}
