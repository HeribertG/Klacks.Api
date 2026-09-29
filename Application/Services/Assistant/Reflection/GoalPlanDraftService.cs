// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default IGoalPlanDraftService. Drafts an AgentPlan from an approved GoalCandidate via
/// IPlanChatService.DraftPlanAsync, persists it in status "drafting" only once it has at least one
/// step (it never runs it), then links the candidate to the new plan via GoalCandidate.PlanId. Refuses to draft when the
/// candidate is missing, not yet Approved, already linked to a plan (PlanId set, so the same candidate
/// is never drafted twice), or BackgroundServiceOptions.GoalReflectionPlanDrafting is off — checked
/// here too, not only at the GoalCandidatesController call site, so any future caller of this service
/// (a Phase 4 retry sweep, another controller) is still gated. A plan that comes back with zero steps
/// (PlanningAgent could not decompose the goal) is treated as a drafting failure: PlanId is left null
/// so a later retry is not blocked by the "already drafted" guard, and the step-less plan is never
/// persisted, so no orphan "drafting" plan accumulates on each retry, matching how CreatePlanSkill treats
/// a step-less plan as an error rather than a result to link. Any failure while drafting is caught and
/// logged; the caller always gets null rather than an exception, because this runs on a fire-and-forget
/// background path that must never take down the request that triggered it.
/// </summary>
/// <param name="goalCandidateRepository">Loads the candidate and persists the PlanId link.</param>
/// <param name="planChatService">Decomposes the goal text into an unpersisted, non-executed AgentPlan draft.</param>
/// <param name="planRepository">Persists the drafted plan once it is known to contain steps.</param>
/// <param name="options">Feature flag gating the draft; see GoalReflectionPlanDrafting.</param>
/// <param name="logger">Structured log of drafted plans and drafting failures.</param>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.Services.Assistant.Planning;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Application.Services.Assistant.Reflection;

public class GoalPlanDraftService : IGoalPlanDraftService
{
    private const string GoalTextSeparator = ": ";

    private readonly IGoalCandidateRepository _goalCandidateRepository;
    private readonly IPlanChatService _planChatService;
    private readonly IAgentPlanRepository _planRepository;
    private readonly BackgroundServiceOptions _options;
    private readonly ILogger<GoalPlanDraftService> _logger;

    public GoalPlanDraftService(
        IGoalCandidateRepository goalCandidateRepository,
        IPlanChatService planChatService,
        IAgentPlanRepository planRepository,
        IOptions<BackgroundServiceOptions> options,
        ILogger<GoalPlanDraftService> logger)
    {
        _goalCandidateRepository = goalCandidateRepository;
        _planChatService = planChatService;
        _planRepository = planRepository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Guid?> DraftForCandidateAsync(Guid candidateId, CancellationToken cancellationToken = default)
    {
        if (!_options.GoalReflectionPlanDrafting)
        {
            return null;
        }

        try
        {
            var candidate = await _goalCandidateRepository.GetByIdAsync(candidateId, cancellationToken);
            if (candidate == null || candidate.Status != GoalCandidateStatus.Approved || candidate.PlanId != null)
            {
                return null;
            }

            var goal = candidate.Title + GoalTextSeparator + candidate.Rationale;
            var plan = await _planChatService.DraftPlanAsync(
                goal,
                candidate.UserId ?? string.Empty,
                sessionId: null,
                cancellationToken: cancellationToken,
                origin: AgentPlanOrigin.SelfReflection);

            var stepCount = PlanStepsJson.CountSteps(plan.StepsJson);
            if (stepCount == 0)
            {
                _logger.LogWarning(
                    "Plan drafted for goal candidate {CandidateId} has zero steps; treating as a " +
                    "drafting failure, not persisting it and leaving PlanId unset so a retry is not blocked",
                    candidateId);
                return null;
            }

            await _planRepository.AddAsync(plan, cancellationToken);

            candidate.PlanId = plan.Id;
            await _goalCandidateRepository.UpdateAsync(candidate, cancellationToken);

            _logger.LogInformation(
                "Drafted plan {PlanId} with {StepCount} step(s) for goal candidate {CandidateId}",
                plan.Id, stepCount, candidateId);

            return plan.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to draft a plan for goal candidate {CandidateId}", candidateId);
            return null;
        }
    }
}
