// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Executes an AgentPlan one step at a time and persists status + currentStepIndex after each step. This facade owns
/// the run lifecycle (load, step budget, Executing -> Completed/Failed/PausedForApproval, cooperative abort) and wires
/// its collaborators in the constructor: PlanRunFactory resolves the effective autonomy level (PlanAutonomyLevelResolver:
/// the user's level for a UserGoal plan, FullyAutonomous for a SelfReflection plan with a still-approved goal candidate)
/// and, on ApproveAndContinueAsync, the resume identity (PlanStepIdentityResolver); PlanStepRunner runs each step through
/// the preview gate, the read-only verify-skill gate and the approval pause (PlanStepRiskGate, decided from the step's
/// skill risk class, never from an LLM-supplied flag; Sensitive steps and unregistered skills always pause, irreversible
/// skills need FullyAutonomous to run unattended), renews the background token, invokes the step and its verify skill
/// with one transient retry each (PlanStepInvoker) and stops the plan on failure; PlanStatusPublisher persists and
/// broadcasts every status change, fires the proactive pause trigger and the task-boundary compaction on completion.
/// Step invocations bypass the chat-level autonomy gate because the plan path enforces its own gate. Cancellation of the
/// supplied token aborts the plan cooperatively (status = aborted). All collaborators log through the logger passed here.
/// </summary>
/// <param name="planRepository">Loads and persists AgentPlan rows.</param>
/// <param name="skillExecutor">Invokes the actual skill implementations.</param>
/// <param name="skillRegistry">Resolves skill descriptors for risk classification.</param>
/// <param name="riskClassifier">Classifies each step's skill so PlanStepApprovalPolicy can decide whether it must pause for approval.</param>
/// <param name="autonomyRepository">Per-user autonomy level used for the pause decision on a UserGoal plan; not read for a SelfReflection plan, which always runs at FullyAutonomous.</param>
/// <param name="notificationService">Broadcasts plan status updates via SignalR.</param>
/// <param name="backgroundTaskService">Fires the fire-and-forget task-boundary compaction trigger on plan completion.</param>
/// <param name="triggerService">Fires the proactive PlanPausedForApprovalTriggerEvent so offline users learn about a pause via the inbox, not only SignalR.</param>
/// <param name="goalCandidateRepository">Proves that a SelfReflection plan traces back to an approved goal candidate before the elevated autonomy level is granted, and supplies the frozen identity ApproveAndContinueAsync resumes under.</param>
/// <param name="internalTokenIssuer">Renews the owner token of a background plan between steps.</param>
/// <param name="previewCatalog">Tells which skills show a server preview and so can only run in the chat after the user confirmed it.</param>
/// <param name="logger">Structured log per step.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Planning;

public class PlanStepExecutor : IPlanStepExecutor
{
    private const int MaxStepBudget = LLMLoopConstants.MaxPlanSteps;

    private readonly IAgentPlanRepository _planRepository;
    private readonly PlanStatusPublisher _publisher;
    private readonly PlanRunFactory _runFactory;
    private readonly PlanStepRunner _stepRunner;
    private readonly ILogger<PlanStepExecutor> _logger;

    public PlanStepExecutor(
        IAgentPlanRepository planRepository,
        ISkillExecutor skillExecutor,
        ISkillRegistry skillRegistry,
        ISkillRiskClassifier riskClassifier,
        IAgentAutonomyPreferenceRepository autonomyRepository,
        IAssistantNotificationService notificationService,
        ILLMBackgroundTaskService backgroundTaskService,
        IAgentTriggerService triggerService,
        IGoalCandidateRepository goalCandidateRepository,
        IInternalTokenIssuer internalTokenIssuer,
        IConfirmationPreviewCatalog previewCatalog,
        ILogger<PlanStepExecutor> logger)
    {
        _planRepository = planRepository;
        _logger = logger;
        _publisher = new PlanStatusPublisher(
            planRepository, notificationService, triggerService, backgroundTaskService, logger);
        var identityResolver = new PlanStepIdentityResolver(internalTokenIssuer, logger);
        _runFactory = new PlanRunFactory(
            goalCandidateRepository, new PlanAutonomyLevelResolver(autonomyRepository, logger), identityResolver);
        _stepRunner = new PlanStepRunner(
            previewCatalog,
            new PlanStepRiskGate(skillRegistry, riskClassifier),
            new PlanStepInvoker(skillExecutor, skillRegistry, logger),
            identityResolver,
            _publisher,
            logger);
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

        var totalSteps = PlanStepListParser.Parse(plan.StepsJson).Count;
        plan.Status = PlanStatus.Aborted;
        plan.LastErrorMessage = null;
        await _publisher.PersistAndPublishAsync(plan, totalSteps, cancellationToken);
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
        var steps = PlanStepListParser.Parse(plan.StepsJson);
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
                await _publisher.PersistAndPublishAsync(plan, totalSteps, cancellationToken);
                _publisher.TriggerTaskBoundaryCompaction(plan);
                return plan;
            }

            plan.Status = PlanStatus.Executing;
            await _publisher.PersistAndPublishAsync(plan, totalSteps, cancellationToken);

            var run = await _runFactory.CreateAsync(plan, skillContext, autoApproveCurrentStep, totalSteps, cancellationToken);
            for (var index = plan.CurrentStepIndex; index < totalSteps; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await _stepRunner.RunAsync(run, index, steps[index], cancellationToken))
                {
                    return plan;
                }
            }

            return await CompleteAsync(plan, totalSteps, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return await AbortCooperativelyAsync(plan, totalSteps);
        }
    }

    private async Task<AgentPlan> CompleteAsync(AgentPlan plan, int totalSteps, CancellationToken cancellationToken)
    {
        plan.Status = PlanStatus.Completed;
        plan.LastErrorMessage = null;
        await _publisher.PersistAndPublishAsync(plan, totalSteps, cancellationToken);
        _logger.LogInformation("Plan {PlanId} completed all {Count} step(s)", plan.Id, totalSteps);
        _publisher.TriggerTaskBoundaryCompaction(plan);
        return plan;
    }

    private async Task<AgentPlan> AbortCooperativelyAsync(AgentPlan plan, int totalSteps)
    {
        plan.Status = PlanStatus.Aborted;
        plan.LastErrorMessage = null;
        await _publisher.PersistAndPublishAsync(plan, totalSteps, CancellationToken.None);
        _logger.LogInformation("Plan {PlanId} aborted cooperatively at step {Index}",
            plan.Id, plan.CurrentStepIndex);
        return plan;
    }
}
