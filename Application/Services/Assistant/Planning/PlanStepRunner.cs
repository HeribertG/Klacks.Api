// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Runs one step of a PlanRun and reports whether the loop may go on. The gates run BEFORE the mutation, in this order:
/// a step whose skill shows a server preview (IConfirmationPreviewCatalog: bulk_delete_works and the macro assignment
/// skills) fails the plan with ConfirmationBindingMessages.ChatOnlyStep, because such a skill runs only as the confirmed
/// replay of its own preview in the chat and pausing for an approval that cannot make it succeed would only mislead the
/// user; a verify skill that is not a registered read-only skill fails the plan; a step PlanStepRiskGate flags pauses the
/// plan for approval unless the run still bypasses the pause of its first resumed step. Then the background token is
/// renewed, the step is invoked, its result is stored under its Order, and a declared verify skill is run against the
/// result. A failed step or verify fails the plan unless the run was cancelled, in which case the cancellation propagates
/// to the caller's abort handling. A passed step advances CurrentStepIndex and is persisted.
/// </summary>
/// <param name="previewCatalog">Tells which skills show a server preview and so can only run in the chat</param>
/// <param name="riskGate">Verify-skill validation and the approval decision</param>
/// <param name="invoker">Invokes the step and its verify skill with one transient retry each</param>
/// <param name="identityResolver">Renews the owner token of a background plan before each step</param>
/// <param name="publisher">Persists and broadcasts every status change and fires the pause trigger</param>
/// <param name="logger">The plan executor's logger, so every step log line keeps its category</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Planning;

internal sealed class PlanStepRunner
{
    private const string MissingSkillErrorMessage = "Skill returned no error message";

    private readonly IConfirmationPreviewCatalog _previewCatalog;
    private readonly PlanStepRiskGate _riskGate;
    private readonly PlanStepInvoker _invoker;
    private readonly PlanStepIdentityResolver _identityResolver;
    private readonly PlanStatusPublisher _publisher;
    private readonly ILogger _logger;

    public PlanStepRunner(
        IConfirmationPreviewCatalog previewCatalog,
        PlanStepRiskGate riskGate,
        PlanStepInvoker invoker,
        PlanStepIdentityResolver identityResolver,
        PlanStatusPublisher publisher,
        ILogger logger)
    {
        _previewCatalog = previewCatalog;
        _riskGate = riskGate;
        _invoker = invoker;
        _identityResolver = identityResolver;
        _publisher = publisher;
        _logger = logger;
    }

    /// <param name="run">State of the current run</param>
    /// <param name="index">Zero-based index of the step in the run</param>
    /// <param name="step">The step at that index</param>
    /// <returns>True when the step passed and the loop continues; false when the plan stopped (failed or paused)</returns>
    public async Task<bool> RunAsync(PlanRun run, int index, PlanStep step, CancellationToken cancellationToken)
    {
        if (await TryStopBeforeStepAsync(run, index, step, cancellationToken))
        {
            return false;
        }

        run.EndApprovalBypass();
        run.UseStepContext(await _identityResolver.RefreshBackgroundTokenAsync(run.StepContext, run.Plan.Id, cancellationToken));

        var skillResult = await _invoker.ExecuteStepAsync(step, run.StepResults, run.StepContext, cancellationToken);
        run.StepResults[step.Order] = skillResult.Data;

        if (!skillResult.Success)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await FailAsync(run, index, skillResult.Message ?? MissingSkillErrorMessage, cancellationToken);
            _logger.LogWarning("Plan {PlanId} failed at step {Index} ({Skill}): {Message}",
                run.Plan.Id, index, step.Skill, skillResult.Message);
            return false;
        }

        if (!string.IsNullOrWhiteSpace(step.VerifySkill)
            && !await VerifyAsync(run, index, step, skillResult.Data, cancellationToken))
        {
            return false;
        }

        run.Plan.CurrentStepIndex = index + 1;
        await _publisher.PersistAndPublishAsync(run.Plan, run.TotalSteps, cancellationToken);
        return true;
    }

    private async Task<bool> TryStopBeforeStepAsync(PlanRun run, int index, PlanStep step, CancellationToken cancellationToken)
    {
        var plan = run.Plan;
        if (_previewCatalog.HasPreview(step.Skill))
        {
            await FailAsync(run, index, ConfirmationBindingMessages.ChatOnlyStep, cancellationToken);
            _logger.LogWarning("Plan {PlanId} stopped at step {Index}: {Skill} shows a server preview and runs only in the chat",
                plan.Id, index, step.Skill);
            return true;
        }

        var verifyRejection = _riskGate.ValidateVerifySkill(step);
        if (verifyRejection != null)
        {
            await FailAsync(run, index, verifyRejection, cancellationToken);
            _logger.LogWarning("Plan {PlanId} rejected at step {Index} ({Skill}): {Message}",
                plan.Id, index, step.Skill, verifyRejection);
            return true;
        }

        if (run.BypassApproval || !_riskGate.RequiresApproval(step, run.AutonomyLevel))
        {
            return false;
        }

        plan.CurrentStepIndex = index;
        plan.Status = PlanStatus.PausedForApproval;
        plan.LastErrorMessage = null;
        await _publisher.PersistAndPublishAsync(plan, run.TotalSteps, cancellationToken);
        _logger.LogInformation("Plan {PlanId} paused for approval at step {Index} ({Skill})",
            plan.Id, index, step.Skill);
        await _publisher.FirePlanPausedTriggerAsync(plan, index, run.TotalSteps, step.Skill, cancellationToken);
        return true;
    }

    private async Task<bool> VerifyAsync(
        PlanRun run, int index, PlanStep step, object? mutationData, CancellationToken cancellationToken)
    {
        var verifyResult = await _invoker.ExecuteVerifyAsync(step, mutationData, run.StepContext, cancellationToken);
        if (verifyResult.Success)
        {
            return true;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await FailAsync(run, index, $"Verify '{step.VerifySkill}' failed: {verifyResult.Message}", cancellationToken);
        _logger.LogWarning("Plan {PlanId} verify failed at step {Index} ({Skill}/{Verify}): {Message}",
            run.Plan.Id, index, step.Skill, step.VerifySkill, verifyResult.Message);
        return false;
    }

    private async Task FailAsync(PlanRun run, int index, string message, CancellationToken cancellationToken)
    {
        run.Plan.CurrentStepIndex = index;
        run.Plan.Status = PlanStatus.Failed;
        run.Plan.LastErrorMessage = message;
        await _publisher.PersistAndPublishAsync(run.Plan, run.TotalSteps, cancellationToken);
    }
}
