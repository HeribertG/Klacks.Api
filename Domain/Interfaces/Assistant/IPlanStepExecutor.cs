// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Phase 3 (autonomy roadmap) — Plan-Step-Executor contract.
/// Runs an AgentPlan one step at a time, pairs each mutating step with its verify-skill if defined,
/// pauses for HITL approval on non-reversible steps, and persists the plan status after each step.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IPlanStepExecutor
{
    /// <summary>
    /// Executes the plan from its current step index until completion, failure or HITL pause.
    /// Returns the final plan state (status / current step index updated).
    /// </summary>
    Task<AgentPlan> ExecutePlanAsync(
        Guid planId,
        SkillExecutionContext skillContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the plan as approved-to-continue past the current step and runs the rest.
    /// No-op if the plan is not in 'paused_for_approval' status.
    /// </summary>
    Task<AgentPlan> ApproveAndContinueAsync(
        Guid planId,
        SkillExecutionContext skillContext,
        CancellationToken cancellationToken = default);
}
