// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Iteration / step limits for the LLM chat multi-turn loop and the autonomy plan executor.
/// Kept here so chat-loop tuning and plan-budget tuning live in one place instead of being
/// scattered as magic numbers across LLMService and PlanStepExecutor.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class LLMLoopConstants
{
    // Guided workflows chain several tool round-trips before answering (e.g. assigning a
    // contract does lookup_location + list_contracts + list_groups, then needs one more
    // iteration to present the choices). A cap of 3 exhausted the budget on the lookups and
    // the loop ended before the assistant could present results — the user saw a "hang".
    public const int MaxChatToolIterations = 6;

    public const int MaxPlanSteps = 15;
}
