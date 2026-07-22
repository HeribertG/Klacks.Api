// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Executes an AgentPlan one step at a time: resolves $prev.X placeholders against earlier step results,
/// invokes each skill via ISkillExecutor, pairs with verify-skill when present, pauses on non-reversible
/// steps until ApproveAndContinueAsync is called, and persists status + currentStepIndex after each step.
/// At autonomy level FullyAutonomous the non-reversible pause is skipped; sensitive skills always pause.
/// Step invocations bypass the chat-level autonomy gate because this executor enforces its own gate.
/// The verify-skill receives the preceding mutation's RESULT payload (e.g. the created entity id), not
/// the mutation's own input parameters. The payload is flattened case-insensitively and then run through
/// <see cref="VerifyParamNameBridge"/>, which fills a verify parameter whose name does not match a result
/// key by case alone (e.g. result 'EmployeeId' -> verify 'clientId') using a curated alias table and a
/// generic-id rule; ambiguous cases are logged and left unbridged rather than guessed. Each skill
/// invocation gets one transient retry (rate-limit /
/// gateway blip), reusing the LLMRetryConstants classification + backoff. Cancellation of the supplied
/// token between steps aborts the plan cooperatively (status = aborted).
/// </summary>
/// <param name="planRepository">Loads and persists AgentPlan rows.</param>
/// <param name="skillExecutor">Invokes the actual skill implementations.</param>
/// <param name="skillRegistry">Resolves skill descriptors for risk classification.</param>
/// <param name="riskClassifier">Classifies steps for the sensitive-always-pause rule.</param>
/// <param name="autonomyRepository">Per-user autonomy level used for the pause decision.</param>
/// <param name="logger">Structured log per step.</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Planning;

public class PlanStepExecutor : IPlanStepExecutor
{
    private const string PrevPlaceholderPrefix = "$prev.";
    private const int MaxStepBudget = LLMLoopConstants.MaxPlanSteps;

    private readonly IAgentPlanRepository _planRepository;
    private readonly ISkillExecutor _skillExecutor;
    private readonly ISkillRegistry _skillRegistry;
    private readonly ISkillRiskClassifier _riskClassifier;
    private readonly IAgentAutonomyPreferenceRepository _autonomyRepository;
    private readonly IAssistantNotificationService _notificationService;
    private readonly ILogger<PlanStepExecutor> _logger;

    public PlanStepExecutor(
        IAgentPlanRepository planRepository,
        ISkillExecutor skillExecutor,
        ISkillRegistry skillRegistry,
        ISkillRiskClassifier riskClassifier,
        IAgentAutonomyPreferenceRepository autonomyRepository,
        IAssistantNotificationService notificationService,
        ILogger<PlanStepExecutor> logger)
    {
        _planRepository = planRepository;
        _skillExecutor = skillExecutor;
        _skillRegistry = skillRegistry;
        _riskClassifier = riskClassifier;
        _autonomyRepository = autonomyRepository;
        _notificationService = notificationService;
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

    public async Task<AgentPlan?> AbortAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await _planRepository.GetByIdAsync(planId, cancellationToken);
        if (plan == null || PlanStatus.IsTerminal(plan.Status))
        {
            return null;
        }

        var totalSteps = ParseSteps(plan.StepsJson).Count;
        plan.Status = PlanStatus.Aborted;
        plan.LastErrorMessage = null;
        await PersistAndPublishAsync(plan, totalSteps, cancellationToken);
        _logger.LogInformation("Plan {PlanId} aborted while not actively running (was {Status})",
            plan.Id, plan.Status);
        return plan;
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

        var totalSteps = steps.Count;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (totalSteps == 0)
            {
                plan.Status = PlanStatus.Completed;
                plan.LastErrorMessage = null;
                await PersistAndPublishAsync(plan, totalSteps, cancellationToken);
                return plan;
            }

            plan.Status = PlanStatus.Executing;
            await PersistAndPublishAsync(plan, totalSteps, cancellationToken);

            var stepResults = new Dictionary<int, object?>();
            var allowedToBypassReversibleGate = autoApproveCurrentStep;
            var autonomyLevel = await GetAutonomyLevelAsync(skillContext.UserId, cancellationToken);
            var stepContext = skillContext with { BypassAutonomyGate = true };

            for (var index = plan.CurrentStepIndex; index < totalSteps; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var step = steps[index];

                if (!allowedToBypassReversibleGate && RequiresApproval(step, autonomyLevel))
                {
                    plan.CurrentStepIndex = index;
                    plan.Status = PlanStatus.PausedForApproval;
                    plan.LastErrorMessage = null;
                    await PersistAndPublishAsync(plan, totalSteps, cancellationToken);
                    _logger.LogInformation("Plan {PlanId} paused for approval at step {Index} ({Skill})",
                        plan.Id, index, step.Skill);
                    return plan;
                }

                allowedToBypassReversibleGate = false;

                var skillResult = await ExecuteSingleStepAsync(step, stepResults, stepContext, cancellationToken);
                stepResults[step.Order] = skillResult.Data;

                if (!skillResult.Success)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    plan.CurrentStepIndex = index;
                    plan.Status = PlanStatus.Failed;
                    plan.LastErrorMessage = skillResult.Message ?? "Skill returned no error message";
                    await PersistAndPublishAsync(plan, totalSteps, cancellationToken);
                    _logger.LogWarning("Plan {PlanId} failed at step {Index} ({Skill}): {Message}",
                        plan.Id, index, step.Skill, skillResult.Message);
                    return plan;
                }

                if (!string.IsNullOrWhiteSpace(step.VerifySkill))
                {
                    var verifyResult = await ExecuteVerifyStepAsync(
                        step, skillResult.Data, stepContext, cancellationToken);
                    if (!verifyResult.Success)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        plan.CurrentStepIndex = index;
                        plan.Status = PlanStatus.Failed;
                        plan.LastErrorMessage = $"Verify '{step.VerifySkill}' failed: {verifyResult.Message}";
                        await PersistAndPublishAsync(plan, totalSteps, cancellationToken);
                        _logger.LogWarning("Plan {PlanId} verify failed at step {Index} ({Skill}/{Verify}): {Message}",
                            plan.Id, index, step.Skill, step.VerifySkill, verifyResult.Message);
                        return plan;
                    }
                }

                plan.CurrentStepIndex = index + 1;
                await PersistAndPublishAsync(plan, totalSteps, cancellationToken);
            }

            plan.Status = PlanStatus.Completed;
            plan.LastErrorMessage = null;
            await PersistAndPublishAsync(plan, totalSteps, cancellationToken);
            _logger.LogInformation("Plan {PlanId} completed all {Count} step(s)", plan.Id, totalSteps);
            return plan;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            plan.Status = PlanStatus.Aborted;
            plan.LastErrorMessage = null;
            await PersistAndPublishAsync(plan, totalSteps, CancellationToken.None);
            _logger.LogInformation("Plan {PlanId} aborted cooperatively at step {Index}",
                plan.Id, plan.CurrentStepIndex);
            return plan;
        }
    }

    private bool RequiresApproval(PlanStep step, AutonomyLevel autonomyLevel)
    {
        var descriptor = _skillRegistry.GetSkillByName(step.Skill);
        if (descriptor != null && _riskClassifier.Classify(descriptor) == SkillRiskClass.Sensitive)
        {
            return true;
        }

        if (step.Reversible)
        {
            return false;
        }

        return autonomyLevel < AutonomyLevel.FullyAutonomous;
    }

    private async Task<AutonomyLevel> GetAutonomyLevelAsync(Guid userId, CancellationToken cancellationToken)
    {
        var row = await _autonomyRepository.GetAsync(userId.ToString(), cancellationToken);
        return row?.Level ?? AutonomyDefaults.DefaultLevel;
    }

    private async Task PersistAndPublishAsync(AgentPlan plan, int totalSteps, CancellationToken cancellationToken)
    {
        await _planRepository.UpdateAsync(plan, cancellationToken);
        if (!string.IsNullOrEmpty(plan.UserId))
        {
            try
            {
                await _notificationService.SendPlanUpdateAsync(
                    plan.UserId,
                    plan.Id,
                    plan.Status,
                    plan.CurrentStepIndex,
                    totalSteps,
                    plan.LastErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Plan {PlanId} update broadcast failed (status={Status})", plan.Id, plan.Status);
            }
        }
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
        return await ExecuteWithTransientRetryAsync(invocation, skillContext, cancellationToken);
    }

    private async Task<SkillResult> ExecuteVerifyStepAsync(
        PlanStep step,
        object? mutationData,
        SkillExecutionContext skillContext,
        CancellationToken cancellationToken)
    {
        var declaredParamNames = ResolveVerifyParamNames(step.VerifySkill!);
        var parameters = BuildVerifyParameters(step, mutationData, declaredParamNames, out var bridgeNotes);
        foreach (var note in bridgeNotes)
        {
            _logger.LogInformation("Plan verify '{Verify}' parameter bridge: {Note}", step.VerifySkill, note);
        }

        var invocation = new SkillInvocation
        {
            SkillName = step.VerifySkill!,
            Parameters = parameters
        };
        return await ExecuteWithTransientRetryAsync(invocation, skillContext, cancellationToken);
    }

    private IReadOnlyList<string> ResolveVerifyParamNames(string verifySkill)
    {
        var descriptor = _skillRegistry.GetSkillByName(verifySkill);
        if (descriptor?.Parameters == null || descriptor.Parameters.Count == 0)
        {
            return Array.Empty<string>();
        }

        return descriptor.Parameters.Select(p => p.Name).ToList();
    }

    private async Task<SkillResult> ExecuteWithTransientRetryAsync(
        SkillInvocation invocation,
        SkillExecutionContext skillContext,
        CancellationToken cancellationToken)
    {
        var result = await _skillExecutor.ExecuteAsync(invocation, skillContext, cancellationToken);

        for (var attempt = 1;
             !result.Success
                && attempt <= LLMLoopConstants.MaxPlanStepTransientRetries
                && TransientProviderErrorDetector.IsTransient(result.Message);
             attempt++)
        {
            _logger.LogWarning(
                "Plan step {Skill} transient failure (attempt {Attempt}/{Max}): {Message}",
                invocation.SkillName, attempt, LLMLoopConstants.MaxPlanStepTransientRetries, result.Message);
            await Task.Delay(LLMRetryConstants.GetRetryDelay(attempt), cancellationToken);
            result = await _skillExecutor.ExecuteAsync(invocation, skillContext, cancellationToken);
        }

        return result;
    }

    private static Dictionary<string, object> BuildVerifyParameters(
        PlanStep step,
        object? mutationData,
        IReadOnlyList<string> declaredVerifyParamNames,
        out IReadOnlyList<string> bridgeNotes)
    {
        var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in FlattenPayloadToParameters(mutationData))
        {
            parameters[key] = value;
        }

        var bridge = VerifyParamNameBridge.BuildAliases(parameters, declaredVerifyParamNames);
        bridgeNotes = bridge.Notes;
        foreach (var (paramName, value) in bridge.Aliases)
        {
            if (!parameters.ContainsKey(paramName))
            {
                parameters[paramName] = value;
            }
        }

        foreach (var (key, value) in step.Params)
        {
            if (value is string s && s.StartsWith(PrevPlaceholderPrefix, StringComparison.Ordinal))
            {
                continue;
            }
            if (value != null)
            {
                parameters[key] = value;
            }
        }

        return parameters;
    }

    private static IEnumerable<KeyValuePair<string, object>> FlattenPayloadToParameters(object? payload)
    {
        if (payload == null)
        {
            yield break;
        }

        if (payload is IDictionary<string, object?> nullableDict)
        {
            foreach (var (key, value) in nullableDict)
            {
                if (value != null)
                {
                    yield return new KeyValuePair<string, object>(key, value);
                }
            }
            yield break;
        }

        if (payload is IDictionary<string, object> dict)
        {
            foreach (var (key, value) in dict)
            {
                yield return new KeyValuePair<string, object>(key, value);
            }
            yield break;
        }

        if (payload is string || payload.GetType().IsPrimitive)
        {
            yield break;
        }

        foreach (var property in payload.GetType().GetProperties(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
            {
                continue;
            }
            var value = property.GetValue(payload);
            if (value != null)
            {
                yield return new KeyValuePair<string, object>(property.Name, value);
            }
        }
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
