// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Invokes the skills of one plan step through ISkillExecutor: the step itself with its "$prev." placeholders
/// resolved against the earlier step results, and its verify skill with the parameters assembled by
/// PlanVerifyParameterBuilder. Each invocation gets one transient retry (rate-limit / gateway blip), reusing the
/// TransientProviderErrorDetector classification and the LLMRetryConstants backoff.
/// </summary>
/// <param name="skillExecutor">Runs the actual skill implementations</param>
/// <param name="skillRegistry">Supplies the verify skill's declared parameter names for the alias bridge</param>
/// <param name="logger">The plan executor's logger, so retry and bridge lines keep its category</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Planning;

internal sealed class PlanStepInvoker
{
    private readonly ISkillExecutor _skillExecutor;
    private readonly ISkillRegistry _skillRegistry;
    private readonly ILogger _logger;

    public PlanStepInvoker(ISkillExecutor skillExecutor, ISkillRegistry skillRegistry, ILogger logger)
    {
        _skillExecutor = skillExecutor;
        _skillRegistry = skillRegistry;
        _logger = logger;
    }

    public async Task<SkillResult> ExecuteStepAsync(
        PlanStep step,
        IReadOnlyDictionary<int, object?> stepResults,
        SkillExecutionContext skillContext,
        CancellationToken cancellationToken)
    {
        var parameters = PlanStepPlaceholderResolver.ResolveParameters(step.Params, stepResults);
        var invocation = new SkillInvocation
        {
            SkillName = step.Skill,
            Parameters = parameters
        };
        return await ExecuteWithTransientRetryAsync(invocation, skillContext, cancellationToken);
    }

    public async Task<SkillResult> ExecuteVerifyAsync(
        PlanStep step,
        object? mutationData,
        SkillExecutionContext skillContext,
        CancellationToken cancellationToken)
    {
        var declaredParamNames = ResolveVerifyParamNames(step.VerifySkill!);
        var parameters = PlanVerifyParameterBuilder.Build(step, mutationData, declaredParamNames, out var bridgeNotes);
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
}
