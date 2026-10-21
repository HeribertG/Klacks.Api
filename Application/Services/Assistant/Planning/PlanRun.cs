// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// State of one RunLoopAsync call: the plan, its (budget-truncated) step count, the autonomy level the pauses are decided
/// at, the context the next step runs under (replaced by every token renewal, so a renewal carries to the following
/// steps), the results so far keyed by step Order, and whether the approval pause of the first resumed step is still
/// bypassed.
/// </summary>
/// <param name="plan">The plan being executed; mutated and persisted step by step</param>
/// <param name="totalSteps">Number of steps this run executes at most, as published with every status change</param>
/// <param name="autonomyLevel">Effective autonomy level from PlanAutonomyLevelResolver</param>
/// <param name="stepContext">Initial step context, already with the chat autonomy gate bypassed and UI actions off</param>
/// <param name="bypassApproval">True on ApproveAndContinueAsync: the step the plan paused at runs without pausing again</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Planning;

internal sealed class PlanRun
{
    public PlanRun(
        AgentPlan plan,
        int totalSteps,
        AutonomyLevel autonomyLevel,
        SkillExecutionContext stepContext,
        bool bypassApproval)
    {
        Plan = plan;
        TotalSteps = totalSteps;
        AutonomyLevel = autonomyLevel;
        StepContext = stepContext;
        BypassApproval = bypassApproval;
    }

    public AgentPlan Plan { get; }

    public int TotalSteps { get; }

    public AutonomyLevel AutonomyLevel { get; }

    public SkillExecutionContext StepContext { get; private set; }

    public bool BypassApproval { get; private set; }

    public Dictionary<int, object?> StepResults { get; } = new();

    public void EndApprovalBypass() => BypassApproval = false;

    public void UseStepContext(SkillExecutionContext stepContext) => StepContext = stepContext;
}
