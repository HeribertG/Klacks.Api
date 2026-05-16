// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Iteration / step limits for the LLM chat multi-turn loop and the autonomy plan executor.
/// Kept here so chat-loop tuning and plan-budget tuning live in one place instead of being
/// scattered as magic numbers across LLMService and PlanStepExecutor.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class LLMLoopConstants
{
    public const int MaxChatToolIterations = 3;

    public const int MaxPlanSteps = 15;
}
