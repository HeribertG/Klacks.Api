// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Chat skill that turns a free-text goal into a multi-step AgentPlan. On the first call it decomposes
/// and persists the plan as a draft, renders the steps as a numbered proposal and stores the EXECUTION
/// as a one-time pending confirmation (override-flag pattern) — it never runs the plan. Only after the
/// user confirms and confirm_pending_action replays this skill with the override flag does the
/// fire-and-forget execution start (same launch path the AgentPlansController uses). The token is
/// marked as issued-this-turn so it cannot be redeemed in the same turn it was proposed.
/// </summary>
/// <param name="planChatService">Shared create-and-start plan lifecycle.</param>
/// <param name="planRepository">Loads the drafted plan on the confirmed execution replay.</param>
/// <param name="confirmationStore">Mints the one-time execution confirmation token.</param>
/// <param name="turnScope">Blocks same-turn redemption of the freshly issued token.</param>

using System.Text;
using System.Text.Json;
using Klacks.Api.Application.Services.Assistant.Planning;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_plan")]
public class CreatePlanSkill : BaseSkillImplementation
{
    private readonly IPlanChatService _planChatService;
    private readonly IAgentPlanRepository _planRepository;
    private readonly IPendingConfirmationStore _confirmationStore;
    private readonly ITurnConfirmationScope _turnScope;

    public CreatePlanSkill(
        IPlanChatService planChatService,
        IAgentPlanRepository planRepository,
        IPendingConfirmationStore confirmationStore,
        ITurnConfirmationScope turnScope)
    {
        _planChatService = planChatService;
        _planRepository = planRepository;
        _confirmationStore = confirmationStore;
        _turnScope = turnScope;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var executeConfirmed = string.Equals(
            GetParameter<string>(parameters, PlanSkillDefaults.ExecuteConfirmedParameter),
            PlanSkillDefaults.ExecuteConfirmedValue,
            StringComparison.OrdinalIgnoreCase);

        if (executeConfirmed)
        {
            return await StartConfirmedExecutionAsync(context, parameters, cancellationToken);
        }

        return await ProposePlanAsync(context, parameters, cancellationToken);
    }

    private async Task<SkillResult> ProposePlanAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        var goal = GetParameter<string>(parameters, PlanSkillDefaults.GoalParameter);
        if (string.IsNullOrWhiteSpace(goal))
        {
            return SkillResult.Error(
                "Cannot draft a plan yet: the 'goal' is missing. Summarise the whole multi-step request " +
                "in one sentence and call create_plan again with it.");
        }

        var sessionId = Guid.TryParse(context.SessionId, out var s) ? s : (Guid?)null;

        var plan = await _planChatService.CreatePlanAsync(
            goal, context.UserId.ToString(), sessionId, cancellationToken);

        var steps = ParseStepLabels(plan.StepsJson);
        if (steps.Count == 0)
        {
            return SkillResult.Error(
                "I could not break this goal down into concrete steps from the available skills. " +
                "Please rephrase the request or split it into individual actions.");
        }

        var pendingParameters = new Dictionary<string, object>
        {
            [PlanSkillDefaults.GoalParameter] = goal,
            [PlanSkillDefaults.PlanIdParameter] = plan.Id.ToString(),
            [PlanSkillDefaults.ExecuteConfirmedParameter] = PlanSkillDefaults.ExecuteConfirmedValue
        };

        var token = _confirmationStore.Create(context.UserId, PlanSkillDefaults.CreatePlanSkillName, pendingParameters);
        _turnScope.MarkIssuedForSensitiveSkill(token);

        var message = new StringBuilder();
        message.Append("I drafted a ").Append(steps.Count).Append(steps.Count == 1 ? " step" : " steps")
            .Append(" plan for this goal. The plan is stored and NOT executed yet:\n");
        for (var i = 0; i < steps.Count; i++)
        {
            message.Append(i + 1).Append(". ").AppendLine(steps[i]);
        }
        message.Append(
            "Ask the user to confirm they want to run this plan; only after they confirm in their own words, call '")
            .Append(AutonomyDefaults.ConfirmPendingActionSkillName).Append("' with '")
            .Append(AutonomyDefaults.ConfirmationTokenParameter).Append("' set to '").Append(token)
            .Append("'. Never confirm or start the plan on your own.");

        return SkillResult.Confirmation(
            message.ToString(),
            token,
            new { planId = plan.Id, stepCount = steps.Count });
    }

    private async Task<SkillResult> StartConfirmedExecutionAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        var planIdRaw = GetParameter<string>(parameters, PlanSkillDefaults.PlanIdParameter);
        if (!Guid.TryParse(planIdRaw, out var planId))
        {
            return SkillResult.Error(
                "Cannot start the plan: the confirmed plan reference is missing or invalid. " +
                "Draft the plan again with create_plan.");
        }

        var plan = await _planRepository.GetByIdAsync(planId, cancellationToken);
        if (plan == null)
        {
            return SkillResult.Error(
                "The drafted plan no longer exists. Draft it again with create_plan.");
        }

        if (!string.Equals(plan.UserId, context.UserId.ToString(), StringComparison.Ordinal))
        {
            return SkillResult.Error("This plan belongs to another user and cannot be started.");
        }

        if (!string.Equals(plan.Status, PlanStatus.Drafting, StringComparison.Ordinal))
        {
            return SkillResult.SuccessResult(
                new { planId = plan.Id, status = plan.Status },
                $"The plan is already '{plan.Status}' — not started again.");
        }

        var providerResolution = await _planChatService.ResolveExecutionProviderAsync(cancellationToken);
        if (!providerResolution.HasDefaultModel)
        {
            return SkillResult.Error(
                "No default LLM model is configured. Set a default model before starting a plan.");
        }

        var executionContext = context with { ProviderId = providerResolution.ProviderId };
        _planChatService.StartBackgroundExecution(plan.Id, executionContext, resume: false);

        return SkillResult.SuccessResult(
            new { planId = plan.Id, status = PlanStatus.Executing },
            "Plan execution started. Progress will stream to the plan panel; tell the user it is running now.");
    }

    private static List<string> ParseStepLabels(string stepsJson)
    {
        var labels = new List<string>();
        if (string.IsNullOrWhiteSpace(stepsJson) || stepsJson == "[]")
        {
            return labels;
        }

        try
        {
            using var doc = JsonDocument.Parse(stepsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return labels;
            }

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var skill = ReadString(element, "Skill", "skill");
                if (string.IsNullOrWhiteSpace(skill))
                {
                    continue;
                }

                var verify = ReadString(element, "VerifySkill", "verifySkill");
                var label = string.IsNullOrWhiteSpace(verify)
                    ? skill
                    : $"{skill} (verified with {verify})";
                labels.Add(label!);
            }
        }
        catch (JsonException)
        {
            return labels;
        }

        return labels;
    }

    private static string? ReadString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }

        return null;
    }
}
