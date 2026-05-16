// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Executes an AgentPlan one step at a time: resolves $prev.X placeholders against earlier step results,
/// invokes each skill via ISkillExecutor, pairs with verify-skill when present, pauses on non-reversible
/// steps until ApproveAndContinueAsync is called, and persists status + currentStepIndex after each step.
/// </summary>
/// <param name="planRepository">Loads and persists AgentPlan rows.</param>
/// <param name="skillExecutor">Invokes the actual skill implementations.</param>
/// <param name="logger">Structured log per step.</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Planning;

public class PlanStepExecutor : IPlanStepExecutor
{
    private const string PrevPlaceholderPrefix = "$prev.";
    private const int MaxStepBudget = LLMLoopConstants.MaxPlanSteps;

    private readonly IAgentPlanRepository _planRepository;
    private readonly ISkillExecutor _skillExecutor;
    private readonly ILogger<PlanStepExecutor> _logger;

    public PlanStepExecutor(
        IAgentPlanRepository planRepository,
        ISkillExecutor skillExecutor,
        ILogger<PlanStepExecutor> logger)
    {
        _planRepository = planRepository;
        _skillExecutor = skillExecutor;
        _logger = logger;
    }

    public async Task<AgentPlan> ExecutePlanAsync(
        Guid planId,
        SkillExecutionContext skillContext,
        CancellationToken cancellationToken = default)
    {
        var plan = await _planRepository.GetByIdAsync(planId, cancellationToken)
            ?? throw new InvalidOperationException($"AgentPlan {planId} not found");

        return await RunLoopAsync(plan, skillContext, autoApproveCurrentStep: false, cancellationToken);
    }

    public async Task<AgentPlan> ApproveAndContinueAsync(
        Guid planId,
        SkillExecutionContext skillContext,
        CancellationToken cancellationToken = default)
    {
        var plan = await _planRepository.GetByIdAsync(planId, cancellationToken)
            ?? throw new InvalidOperationException($"AgentPlan {planId} not found");

        if (plan.Status != PlanStatus.PausedForApproval)
        {
            return plan;
        }

        return await RunLoopAsync(plan, skillContext, autoApproveCurrentStep: true, cancellationToken);
    }

    private async Task<AgentPlan> RunLoopAsync(
        AgentPlan plan,
        SkillExecutionContext skillContext,
        bool autoApproveCurrentStep,
        CancellationToken cancellationToken)
    {
        var steps = ParseSteps(plan.StepsJson);
        if (steps.Count > MaxStepBudget)
        {
            steps = steps.Take(MaxStepBudget).ToList();
        }
        if (steps.Count == 0)
        {
            plan.Status = PlanStatus.Completed;
            plan.LastErrorMessage = null;
            await _planRepository.UpdateAsync(plan, cancellationToken);
            return plan;
        }

        plan.Status = PlanStatus.Executing;
        await _planRepository.UpdateAsync(plan, cancellationToken);

        var stepResults = new Dictionary<int, object?>();
        var allowedToBypassReversibleGate = autoApproveCurrentStep;

        for (var index = plan.CurrentStepIndex; index < steps.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var step = steps[index];

            if (!step.Reversible && !allowedToBypassReversibleGate)
            {
                plan.CurrentStepIndex = index;
                plan.Status = PlanStatus.PausedForApproval;
                plan.LastErrorMessage = null;
                await _planRepository.UpdateAsync(plan, cancellationToken);
                _logger.LogInformation("Plan {PlanId} paused for approval at step {Index} ({Skill})",
                    plan.Id, index, step.Skill);
                return plan;
            }

            allowedToBypassReversibleGate = false;

            var skillResult = await ExecuteSingleStepAsync(step, stepResults, skillContext, cancellationToken);
            stepResults[step.Order] = skillResult.Data;

            if (!skillResult.Success)
            {
                plan.CurrentStepIndex = index;
                plan.Status = PlanStatus.Failed;
                plan.LastErrorMessage = skillResult.Message ?? "Skill returned no error message";
                await _planRepository.UpdateAsync(plan, cancellationToken);
                _logger.LogWarning("Plan {PlanId} failed at step {Index} ({Skill}): {Message}",
                    plan.Id, index, step.Skill, skillResult.Message);
                return plan;
            }

            if (!string.IsNullOrWhiteSpace(step.VerifySkill))
            {
                var verifyResult = await ExecuteVerifyStepAsync(step, stepResults, skillContext, cancellationToken);
                if (!verifyResult.Success)
                {
                    plan.CurrentStepIndex = index;
                    plan.Status = PlanStatus.Failed;
                    plan.LastErrorMessage = $"Verify '{step.VerifySkill}' failed: {verifyResult.Message}";
                    await _planRepository.UpdateAsync(plan, cancellationToken);
                    _logger.LogWarning("Plan {PlanId} verify failed at step {Index} ({Skill}/{Verify}): {Message}",
                        plan.Id, index, step.Skill, step.VerifySkill, verifyResult.Message);
                    return plan;
                }
            }

            plan.CurrentStepIndex = index + 1;
            await _planRepository.UpdateAsync(plan, cancellationToken);
        }

        plan.Status = PlanStatus.Completed;
        plan.LastErrorMessage = null;
        await _planRepository.UpdateAsync(plan, cancellationToken);
        _logger.LogInformation("Plan {PlanId} completed all {Count} step(s)", plan.Id, steps.Count);
        return plan;
    }

    private async Task<SkillResult> ExecuteSingleStepAsync(
        PlanStep step,
        IReadOnlyDictionary<int, object?> stepResults,
        SkillExecutionContext skillContext,
        CancellationToken cancellationToken)
    {
        var parameters = ResolveParameters(step.Params, stepResults);
        var invocation = new SkillInvocation
        {
            SkillName = step.Skill,
            Parameters = parameters
        };
        return await _skillExecutor.ExecuteAsync(invocation, skillContext, cancellationToken);
    }

    private async Task<SkillResult> ExecuteVerifyStepAsync(
        PlanStep step,
        IReadOnlyDictionary<int, object?> stepResults,
        SkillExecutionContext skillContext,
        CancellationToken cancellationToken)
    {
        var parameters = ResolveParameters(step.Params, stepResults);
        var invocation = new SkillInvocation
        {
            SkillName = step.VerifySkill!,
            Parameters = parameters
        };
        return await _skillExecutor.ExecuteAsync(invocation, skillContext, cancellationToken);
    }

    private static Dictionary<string, object> ResolveParameters(
        Dictionary<string, object?> rawParams,
        IReadOnlyDictionary<int, object?> stepResults)
    {
        var resolved = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (rawParams.Count == 0) return resolved;

        var previousData = stepResults.Count > 0
            ? stepResults[stepResults.Keys.Max()]
            : null;

        foreach (var (key, value) in rawParams)
        {
            var resolvedValue = ResolveValue(value, previousData);
            if (resolvedValue != null)
            {
                resolved[key] = resolvedValue;
            }
        }
        return resolved;
    }

    private static object? ResolveValue(object? value, object? previousData)
    {
        if (value is string strValue && strValue.StartsWith(PrevPlaceholderPrefix, StringComparison.Ordinal))
        {
            var path = strValue[PrevPlaceholderPrefix.Length..];
            return ExtractFromObject(previousData, path);
        }
        return value;
    }

    private static object? ExtractFromObject(object? source, string path)
    {
        if (source == null) return null;
        var parts = path.Split('.');

        var current = source;
        foreach (var part in parts)
        {
            current = ExtractMember(current, part);
            if (current == null) return null;
        }
        return current;
    }

    private static object? ExtractMember(object? source, string memberName)
    {
        if (source == null) return null;

        if (source is IDictionary<string, object?> dictNullable && dictNullable.TryGetValue(memberName, out var v1))
        {
            return v1;
        }
        if (source is IDictionary<string, object> dict && dict.TryGetValue(memberName, out var v2))
        {
            return v2;
        }

        var property = source.GetType().GetProperty(memberName,
            System.Reflection.BindingFlags.IgnoreCase |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance);
        return property?.GetValue(source);
    }

    private static List<PlanStep> ParseSteps(string stepsJson)
    {
        if (string.IsNullOrWhiteSpace(stepsJson) || stepsJson == "[]") return [];
        try
        {
            using var doc = JsonDocument.Parse(stepsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return [];

            var list = new List<PlanStep>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object) continue;
                if (!element.TryGetProperty("Skill", out var skillEl) &&
                    !element.TryGetProperty("skill", out skillEl))
                {
                    continue;
                }
                var skill = skillEl.GetString();
                if (string.IsNullOrWhiteSpace(skill)) continue;

                var order = TryGetInt(element, "Order", "order") ?? list.Count + 1;
                var verify = TryGetString(element, "VerifySkill", "verifySkill");
                var reversible = TryGetBool(element, "Reversible", "reversible") ?? false;

                var paramsMap = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                if ((element.TryGetProperty("Params", out var paramsEl) ||
                     element.TryGetProperty("params", out paramsEl)) &&
                    paramsEl.ValueKind == JsonValueKind.Object)
                {
                    foreach (var p in paramsEl.EnumerateObject())
                    {
                        paramsMap[p.Name] = JsonValueAsObject(p.Value);
                    }
                }

                list.Add(new PlanStep(order, skill, paramsMap, verify, reversible));
            }
            return list;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static int? TryGetInt(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i))
            {
                return i;
            }
        }
        return null;
    }

    private static string? TryGetString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String)
            {
                return v.GetString();
            }
        }
        return null;
    }

    private static bool? TryGetBool(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var v))
            {
                if (v.ValueKind == JsonValueKind.True) return true;
                if (v.ValueKind == JsonValueKind.False) return false;
            }
        }
        return null;
    }

    private static object? JsonValueAsObject(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.TryGetInt64(out var l) ? (object)l : value.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => value.GetRawText()
        };
    }
}
