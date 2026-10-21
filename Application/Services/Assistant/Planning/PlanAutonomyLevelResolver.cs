// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the autonomy level a plan run's approval pauses are decided at. A plan with Origin = UserGoal (chat/REST)
/// runs at the user's configured level. A plan with Origin = SelfReflection runs at FullyAutonomous because it was
/// authorized at the goal level: a human approved the GoalCandidate before this plan was ever drafted (Phase 4 of the
/// self-directed-goals feature, see docs/superpowers/specs/2026-07-28-klacksy-selbstgesteuerte-ziele-design.md), so the
/// control point is that approval rather than the user's chat-configured level. The elevation is granted only when that
/// approval can still be proven - an approved candidate pointing at this plan. The origin flag alone is not enough:
/// without this check it would grant the highest level, and any caller reaching ExecutePlanAsync directly would bypass
/// every brake that GoalPlanExecutionService enforces. Falling back to the user's own level fails closed.
/// PlanStepApprovalPolicy's Sensitive floor applies unconditionally either way.
/// </summary>
/// <param name="autonomyRepository">Per-user autonomy level; not read when an approved candidate elevates the run</param>
/// <param name="logger">The plan executor's logger, so the fallback warning keeps its category</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Planning;

internal sealed class PlanAutonomyLevelResolver
{
    private readonly IAgentAutonomyPreferenceRepository _autonomyRepository;
    private readonly ILogger _logger;

    public PlanAutonomyLevelResolver(IAgentAutonomyPreferenceRepository autonomyRepository, ILogger logger)
    {
        _autonomyRepository = autonomyRepository;
        _logger = logger;
    }

    /// <param name="plan">The plan about to run</param>
    /// <param name="goalCandidate">The candidate looked up for a SelfReflection plan, null otherwise</param>
    /// <param name="userId">The caller's user id, whose level is the fallback</param>
    public async Task<AutonomyLevel> ResolveAsync(
        AgentPlan plan, GoalCandidate? goalCandidate, Guid userId, CancellationToken cancellationToken)
    {
        if (plan.Origin != AgentPlanOrigin.SelfReflection)
        {
            return await GetAutonomyLevelAsync(userId, cancellationToken);
        }

        if (goalCandidate?.Status == GoalCandidateStatus.Approved)
        {
            return AutonomyLevel.FullyAutonomous;
        }

        _logger.LogWarning(
            "Plan {PlanId} claims origin {Origin} but has no approved goal candidate; falling back to the user's autonomy level",
            plan.Id, plan.Origin);
        return await GetAutonomyLevelAsync(userId, cancellationToken);
    }

    private async Task<AutonomyLevel> GetAutonomyLevelAsync(Guid userId, CancellationToken cancellationToken)
    {
        var row = await _autonomyRepository.GetAsync(userId.ToString(), cancellationToken);
        return row?.Level ?? AutonomyDefaults.DefaultLevel;
    }
}
