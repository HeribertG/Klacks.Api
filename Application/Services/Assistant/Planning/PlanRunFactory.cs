// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the PlanRun for one RunLoopAsync call. The goal candidate of a SelfReflection plan is looked up once and reused
/// both for the effective autonomy level and for the resume identity, instead of querying IGoalCandidateRepository twice;
/// a UserGoal plan does no lookup. Every step runs with the chat-level autonomy gate bypassed (BypassAutonomyGate = true,
/// the plan path enforces its own gate through PlanStepRiskGate) and without UI actions.
/// </summary>
/// <param name="goalCandidateRepository">Proves that a SelfReflection plan traces back to an approved goal candidate</param>
/// <param name="autonomyResolver">Resolves the effective autonomy level from the plan origin and the candidate</param>
/// <param name="identityResolver">Resolves the identity a resumed SelfReflection plan keeps running under</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Planning;

internal sealed class PlanRunFactory
{
    private readonly IGoalCandidateRepository _goalCandidateRepository;
    private readonly PlanAutonomyLevelResolver _autonomyResolver;
    private readonly PlanStepIdentityResolver _identityResolver;

    public PlanRunFactory(
        IGoalCandidateRepository goalCandidateRepository,
        PlanAutonomyLevelResolver autonomyResolver,
        PlanStepIdentityResolver identityResolver)
    {
        _goalCandidateRepository = goalCandidateRepository;
        _autonomyResolver = autonomyResolver;
        _identityResolver = identityResolver;
    }

    /// <param name="plan">The plan about to run</param>
    /// <param name="skillContext">Context supplied by the caller</param>
    /// <param name="autoApproveCurrentStep">True on ApproveAndContinueAsync</param>
    /// <param name="totalSteps">Budget-truncated step count of this run</param>
    public async Task<PlanRun> CreateAsync(
        AgentPlan plan,
        SkillExecutionContext skillContext,
        bool autoApproveCurrentStep,
        int totalSteps,
        CancellationToken cancellationToken)
    {
        var goalCandidate = plan.Origin == AgentPlanOrigin.SelfReflection
            ? await _goalCandidateRepository.GetByPlanIdAsync(plan.Id, cancellationToken)
            : null;
        var autonomyLevel = await _autonomyResolver.ResolveAsync(
            plan, goalCandidate, skillContext.UserId, cancellationToken);
        var effectiveSkillContext = autoApproveCurrentStep
            ? await _identityResolver.ResolveResumeContextAsync(plan, goalCandidate, skillContext, cancellationToken)
            : skillContext;
        var stepContext = effectiveSkillContext with { BypassAutonomyGate = true, SupportsUiActions = false };

        return new PlanRun(plan, totalSteps, autonomyLevel, stepContext, autoApproveCurrentStep);
    }
}
