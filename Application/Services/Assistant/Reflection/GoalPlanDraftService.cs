// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default IGoalPlanDraftService. Drafts an AgentPlan from an approved GoalCandidate via
/// IPlanChatService.CreatePlanAsync — which persists the plan in status "drafting" and never runs
/// it — then links the candidate to the new plan via GoalCandidate.PlanId. Refuses to draft when the
/// candidate is missing, not yet Approved, already linked to a plan (PlanId set, so the same candidate
/// is never drafted twice), or BackgroundServiceOptions.GoalReflectionPlanDrafting is off — checked
/// here too, not only at the GoalCandidatesController call site, so any future caller of this service
/// (a Phase 4 retry sweep, another controller) is still gated. A plan that comes back with zero steps
/// (PlanningAgent could not decompose the goal) is treated as a drafting failure: PlanId is left null
/// so a later retry is not blocked by the "already drafted" guard, matching how CreatePlanSkill treats
/// a step-less plan as an error rather than a result to link. Any failure while drafting is caught and
/// logged; the caller always gets null rather than an exception, because this runs on a fire-and-forget
/// background path that must never take down the request that triggered it.
/// </summary>
/// <param name="goalCandidateRepository">Loads the candidate and persists the PlanId link.</param>
/// <param name="planChatService">Decomposes the goal text into a persisted, non-executed AgentPlan.</param>
/// <param name="options">Feature flag gating the draft; see GoalReflectionPlanDrafting.</param>
/// <param name="logger">Structured log of drafted plans and drafting failures.</param>

using System.Text.Json;
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
    private readonly BackgroundServiceOptions _options;
    private readonly ILogger<GoalPlanDraftService> _logger;

    public GoalPlanDraftService(
        IGoalCandidateRepository goalCandidateRepository,
        IPlanChatService planChatService,
        IOptions<BackgroundServiceOptions> options,
        ILogger<GoalPlanDraftService> logger)
    {
        _goalCandidateRepository = goalCandidateRepository;
        _planChatService = planChatService;
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
            var plan = await _planChatService.CreatePlanAsync(
                goal,
                candidate.UserId ?? string.Empty,
                sessionId: null,
                cancellationToken: cancellationToken,
                origin: AgentPlanOrigin.SelfReflection);

            var stepCount = CountSteps(plan.StepsJson);
            if (stepCount == 0)
            {
                _logger.LogWarning(
                    "Plan {PlanId} drafted for goal candidate {CandidateId} has zero steps; treating as a " +
                    "drafting failure and leaving PlanId unset so a retry is not blocked",
                    plan.Id, candidateId);
                return null;
            }

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

    private static int CountSteps(string stepsJson)
    {
        if (string.IsNullOrWhiteSpace(stepsJson) || stepsJson == "[]")
        {
            return 0;
        }

        try
        {
            using var doc = JsonDocument.Parse(stepsJson);
            return doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement.GetArrayLength()
                : 0;
        }
        catch (JsonException)
        {
            return 0;
        }
    }
}
