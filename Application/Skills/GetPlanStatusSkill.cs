// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Chat skill that reports the status of the caller's most recent AgentPlan, or a specific plan when
/// a planId is given: it returns the lifecycle status, the current step position, the total step
/// count and the last error message (when failed or aborted). Read-only; only the caller's own plans
/// are visible.
/// </summary>
/// <param name="planRepository">Loads the caller's plans for status reporting.</param>

using System.Text.Json;
using Klacks.Api.Application.Services.Assistant.Planning;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_plan_status")]
public class GetPlanStatusSkill : BaseSkillImplementation
{
    private readonly IAgentPlanRepository _planRepository;

    public GetPlanStatusSkill(IAgentPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var userId = context.UserId.ToString();
        var planIdRaw = GetParameter<string>(parameters, PlanSkillDefaults.PlanIdParameter);

        AgentPlan? plan;
        if (!string.IsNullOrWhiteSpace(planIdRaw))
        {
            if (!Guid.TryParse(planIdRaw, out var planId))
            {
                return SkillResult.Error($"Invalid planId value: {planIdRaw}.");
            }

            plan = await _planRepository.GetByIdAsync(planId, cancellationToken);
            if (plan == null || !string.Equals(plan.UserId, userId, StringComparison.Ordinal))
            {
                return SkillResult.Error("No plan with that id belongs to you.");
            }
        }
        else
        {
            var plans = await _planRepository.ListByUserAsync(userId, cancellationToken);
            plan = plans.OrderByDescending(p => p.CreateTime).FirstOrDefault();
            if (plan == null)
            {
                return SkillResult.SuccessResult(
                    new { hasPlan = false },
                    "There are no plans yet. Ask create_plan to draft one first.");
            }
        }

        var totalSteps = CountSteps(plan.StepsJson);
        var currentStep = Math.Min(plan.CurrentStepIndex + 1, Math.Max(totalSteps, 1));

        var data = new
        {
            planId = plan.Id,
            status = plan.Status,
            currentStep,
            totalSteps,
            lastError = plan.LastErrorMessage
        };

        var message = PlanStatus.IsTerminal(plan.Status)
            ? $"Plan status: {plan.Status} ({totalSteps} step(s))." +
              (string.IsNullOrWhiteSpace(plan.LastErrorMessage) ? string.Empty : $" Last error: {plan.LastErrorMessage}")
            : $"Plan status: {plan.Status}, at step {currentStep} of {totalSteps}.";

        return SkillResult.SuccessResult(data, message);
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
            return doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement.GetArrayLength() : 0;
        }
        catch (JsonException)
        {
            return 0;
        }
    }
}
