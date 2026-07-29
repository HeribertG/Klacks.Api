// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Phase 4 of the self-directed-goals roadmap: starts the unattended execution of an AgentPlan drafted
/// from an approved GoalCandidate (see
/// docs/superpowers/specs/2026-07-28-klacksy-selbstgesteuerte-ziele-design.md). Execution runs the
/// already-drafted plan at AutonomyLevel.FullyAutonomous under the candidate's frozen owner identity —
/// it never re-plans and never asks a human again, because the human already approved the goal.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IGoalPlanExecutionService
{
    /// <summary>
    /// Checks every execution brake (feature flag, approval/plan state, confidence, minimum admin
    /// autonomy level, frozen owner permissions) and, only if all pass, starts the plan running in the
    /// background. Returns true when execution was started, false when any brake rejected it — the
    /// rejection reason is always logged, never only silently swallowed.
    /// </summary>
    /// <param name="candidateId">Id of the approved, plan-linked GoalCandidate to execute.</param>
    Task<bool> ExecuteForCandidateAsync(Guid candidateId, CancellationToken cancellationToken = default);
}
