// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Executes an AgentPlan one step at a time: resolves $prev.X placeholders against earlier step results,
/// invokes each skill via ISkillExecutor, pairs with verify-skill when present, pauses on steps that
/// PlanStepApprovalPolicy flags as requiring approval until ApproveAndContinueAsync is called, and
/// persists status + currentStepIndex after each step. The pause decision comes from the step's skill
/// risk class (via ISkillRiskClassifier) and an effective autonomy level, not from any LLM-supplied flag
/// on the step itself; a skill with no registered descriptor and sensitive skills always pause, irreversible
/// skills additionally require autonomy level FullyAutonomous to run unattended. The effective level is the
/// user's configured autonomy level for a plan with Origin = UserGoal (chat/REST), but is always
/// FullyAutonomous for a plan with Origin = SelfReflection whose goal candidate is still approved
/// (Phase 4 of the self-directed-goals feature, see
/// docs/superpowers/specs/2026-07-28-klacksy-selbstgesteuerte-ziele-design.md) — that plan was authorized
/// when a human approved the GoalCandidate it was drafted from, so the control point already happened at
/// the goal level. The origin flag alone does not grant the elevation: without a matching approved
/// candidate the executor falls back to the user's own level. This does not weaken the Sensitive floor:
/// PlanStepApprovalPolicy still pauses Sensitive steps unconditionally.
/// Step invocations bypass the chat-level autonomy gate because this executor enforces its own gate.
/// A step naming a verify skill that is not a registered READ-ONLY catalogue skill fails the plan before
/// its mutation runs. The verify name arrives verbatim from the LLM and passes neither RequiresApproval
/// nor the bypassed chat gate, so without this a sensitive skill named as verifySkill would execute with
/// no confirmation at all; the check sits before the mutation because after it no safe pause point is
/// left - resuming re-enters the loop body and would repeat the mutation.
/// The verify-skill receives the preceding mutation's RESULT payload (e.g. the created entity id), not
/// the mutation's own input parameters. The payload is flattened case-insensitively and then run through
/// <see cref="VerifyParamNameBridge"/>, which fills a verify parameter whose name does not match a result
/// key by case alone (e.g. result 'EmployeeId' -> verify 'clientId') using a curated alias table and a
/// generic-id rule; ambiguous cases are logged and left unbridged rather than guessed. Each skill
/// invocation gets one transient retry (rate-limit /
/// gateway blip), reusing the LLMRetryConstants classification + backoff. Cancellation of the supplied
/// token between steps aborts the plan cooperatively (status = aborted). When a plan reaches Completed
/// (not aborted/failed) this also fires a task-boundary conversation compaction with a lower message
/// threshold than the default post-turn trigger, so short plan runs don't compact needlessly; the plan's
/// SessionId (parsed as the chat conversation id) must be present or the trigger is silently skipped.
/// When a step pauses the plan for approval, this also fires a PlanPausedForApprovalTriggerEvent
/// through IAgentTriggerService so an unattended plan (e.g. run overnight by a background service)
/// still reaches the owning user's proactive inbox, not just connected SignalR clients. That trigger
/// call is best-effort: any failure is logged and swallowed, mirroring the SignalR broadcast in
/// PersistAndPublishAsync, and it is skipped (with a log) when plan.UserId is missing or not a Guid.
/// When ApproveAndContinueAsync resumes a SelfReflection plan that has a matching approved GoalCandidate
/// with non-empty frozen permissions, the remaining steps run under that candidate's audit identity - the
/// same GoalSelfReflectionAuditConstants.AuditUserName, frozen OwnerPermissionsCsv, and SessionId prefix
/// that GoalPlanExecutionService used for the original unattended run - rather than under the resuming
/// caller's UserName/UserPermissions/SessionId. The resuming caller's UserId and TenantId are left as
/// supplied (UserId still feeds the autonomy-level fallback below); only the three audit-relevant fields
/// are overridden. Without this, a human clearing a Sensitive-step pause would silently take over the
/// audit trail for the automation's own remaining steps. A plan with Origin = UserGoal is never affected:
/// the supplied context always runs unchanged. The goal candidate is looked up once per RunLoopAsync call
/// and reused both for this identity override and for the effective-autonomy-level decision below,
/// instead of querying IGoalCandidateRepository twice.
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
/// <param name="logger">Structured log per step.</param>

using System.Globalization;
using System.Text.Json;
using Klacks.Api.Application.Services.Assistant.Triggers;
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
    private const int TaskBoundaryMinMessages = 10;
    private const string VerifySkillUnknownMessage =
        "Verify skill '{0}' of step '{1}' is not a registered skill. A verify step must name a read-only "
        + "skill from the catalogue; the plan was stopped before the step ran.";
    private const string VerifySkillNotReadOnlyMessage =
        "Verify skill '{0}' of step '{1}' is classified {2}, but a verify step must be read-only; the plan "
        + "was stopped before the step ran.";

    private readonly IAgentPlanRepository _planRepository;
    private readonly ISkillExecutor _skillExecutor;
    private readonly ISkillRegistry _skillRegistry;
    private readonly ISkillRiskClassifier _riskClassifier;
    private readonly IAgentAutonomyPreferenceRepository _autonomyRepository;
    private readonly IAssistantNotificationService _notificationService;
    private readonly ILLMBackgroundTaskService _backgroundTaskService;
    private readonly IAgentTriggerService _triggerService;
    private readonly IGoalCandidateRepository _goalCandidateRepository;
    private readonly IInternalTokenIssuer _internalTokenIssuer;
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
        ILogger<PlanStepExecutor> logger)
    {
        _planRepository = planRepository;
        _skillExecutor = skillExecutor;
        _skillRegistry = skillRegistry;
        _riskClassifier = riskClassifier;
        _autonomyRepository = autonomyRepository;
        _notificationService = notificationService;
        _backgroundTaskService = backgroundTaskService;
        _triggerService = triggerService;
        _goalCandidateRepository = goalCandidateRepository;
        _internalTokenIssuer = internalTokenIssuer;
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
                TriggerTaskBoundaryCompaction(plan);
                return plan;
            }

            plan.Status = PlanStatus.Executing;
            await PersistAndPublishAsync(plan, totalSteps, cancellationToken);

            var stepResults = new Dictionary<int, object?>();
            var allowedToBypassReversibleGate = autoApproveCurrentStep;
            var goalCandidate = plan.Origin == AgentPlanOrigin.SelfReflection
                ? await _goalCandidateRepository.GetByPlanIdAsync(plan.Id, cancellationToken)
                : null;
            var autonomyLevel = await ResolveEffectiveAutonomyLevelAsync(
                plan, goalCandidate, skillContext.UserId, cancellationToken);
            var effectiveSkillContext = autoApproveCurrentStep
                ? await ResolveResumeExecutionContextAsync(plan, goalCandidate, skillContext, cancellationToken)
                : skillContext;
            var stepContext = effectiveSkillContext with { BypassAutonomyGate = true, SupportsUiActions = false };

            for (var index = plan.CurrentStepIndex; index < totalSteps; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var step = steps[index];

                // Checked BEFORE the mutation runs, not next to the verify call itself: once
                // ExecuteSingleStepAsync has run there is no safe stopping point left, because resuming a
                // paused plan re-enters the loop body at CurrentStepIndex and would repeat the mutation.
                var verifyRejection = ValidateVerifySkill(step);
                if (verifyRejection != null)
                {
                    plan.CurrentStepIndex = index;
                    plan.Status = PlanStatus.Failed;
                    plan.LastErrorMessage = verifyRejection;
                    await PersistAndPublishAsync(plan, totalSteps, cancellationToken);
                    _logger.LogWarning("Plan {PlanId} rejected at step {Index} ({Skill}): {Message}",
                        plan.Id, index, step.Skill, verifyRejection);
                    return plan;
                }

                if (!allowedToBypassReversibleGate && RequiresApproval(step, autonomyLevel))
                {
                    plan.CurrentStepIndex = index;
                    plan.Status = PlanStatus.PausedForApproval;
                    plan.LastErrorMessage = null;
                    await PersistAndPublishAsync(plan, totalSteps, cancellationToken);
                    _logger.LogInformation("Plan {PlanId} paused for approval at step {Index} ({Skill})",
                        plan.Id, index, step.Skill);
                    await FirePlanPausedTriggerAsync(plan, index, totalSteps, step.Skill, cancellationToken);
                    return plan;
                }

                allowedToBypassReversibleGate = false;

                stepContext = await RefreshBackgroundTokenAsync(stepContext, plan.Id, cancellationToken);

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
            TriggerTaskBoundaryCompaction(plan);
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

    /// <summary>
    /// Rejects a verify skill that is not a read-only catalogue skill. The name is taken verbatim from the
    /// LLM response (PlanningAgent) and is never matched against the catalogue, while the verify call runs
    /// with the chat autonomy gate bypassed and without any RequiresApproval check - so a sensitive skill
    /// named as verifySkill would execute unattended. The system prompt already defines a verify step as a
    /// pure read of the mutation's result; this enforces that in code instead of trusting the prompt.
    /// An unregistered name fails too, mirroring the fail-closed <c>descriptor == null</c> arm of
    /// <see cref="RequiresApproval"/>.
    /// </summary>
    /// <param name="step">Step whose VerifySkill is validated; a step without one is always accepted</param>
    /// <returns>The failure message, or null when the step may run</returns>
    private string? ValidateVerifySkill(PlanStep step)
    {
        if (string.IsNullOrWhiteSpace(step.VerifySkill))
        {
            return null;
        }

        var descriptor = _skillRegistry.GetSkillByName(step.VerifySkill);
        if (descriptor == null)
        {
            return string.Format(
                CultureInfo.InvariantCulture, VerifySkillUnknownMessage, step.VerifySkill, step.Skill);
        }

        var riskClass = _riskClassifier.Classify(descriptor);
        return riskClass == SkillRiskClass.ReadOnly
            ? null
            : string.Format(
                CultureInfo.InvariantCulture, VerifySkillNotReadOnlyMessage, step.VerifySkill, step.Skill, riskClass);
    }

    private bool RequiresApproval(PlanStep step, AutonomyLevel autonomyLevel)
    {
        var descriptor = _skillRegistry.GetSkillByName(step.Skill);
        if (descriptor == null)
        {
            return true;
        }

        return PlanStepApprovalPolicy.RequiresApproval(_riskClassifier.Classify(descriptor), autonomyLevel);
    }

    // A SelfReflection plan runs at FullyAutonomous because it was authorized at the goal level: a human
    // approved the GoalCandidate before this plan was ever drafted, so the control point is that approval
    // rather than the user's chat-configured level. The elevation is therefore granted only when that
    // approval can still be proven - an approved candidate pointing at this plan. A plan merely carrying
    // Origin = SelfReflection is not enough: without this check the flag alone would grant the highest
    // level, and any future caller reaching ExecutePlanAsync directly would bypass every brake that
    // GoalPlanExecutionService enforces. Falling back to the user's own level fails closed.
    // PlanStepApprovalPolicy's Sensitive floor applies unconditionally either way.
    private async Task<AutonomyLevel> ResolveEffectiveAutonomyLevelAsync(
        AgentPlan plan, GoalCandidate? goalCandidate, Guid userId, CancellationToken cancellationToken)
    {
        if (plan.Origin != AgentPlanOrigin.SelfReflection)
        {
            return await GetAutonomyLevelAsync(userId, cancellationToken);
        }

        if (goalCandidate?.Status == GoalCandidateStatus.Approved)
        {
            return AutonomyLevel.FullyAutonomous;
        }

        _logger.LogWarning(
            "Plan {PlanId} claims origin {Origin} but has no approved goal candidate; falling back to the user's autonomy level",
            plan.Id, plan.Origin);
        return await GetAutonomyLevelAsync(userId, cancellationToken);
    }

    // ApproveAndContinueAsync resumes a paused plan under whatever SkillExecutionContext its caller
    // supplies - normally the identity of the human clearing the pause. For a SelfReflection plan that
    // is wrong: the remaining steps were authorized by the same approved GoalCandidate that let
    // GoalPlanExecutionService start the plan unattended in the first place, so they must keep running
    // under that candidate's audit name, frozen permissions, and SessionId, or the audit trail silently
    // switches to the resuming human right at the steps that matter most. UserId/TenantId are left as
    // supplied - only the three audit-relevant fields are overridden. Falls back to the supplied context,
    // with a log, when the candidate is missing, not approved, or its frozen permissions are empty - the
    // same fail-closed stance as ResolveEffectiveAutonomyLevelAsync. Only consulted when
    // autoApproveCurrentStep is set (i.e. ApproveAndContinueAsync); ExecutePlanAsync always runs under
    // the context GoalPlanExecutionService already built.
    /// <summary>
    /// Mints a fresh token before each step of a background plan. The internal token lives only minutes,
    /// while a plan step can involve an LLM call or a whole wizard run — without this the later steps of
    /// a long plan would fail with an authentication error instead of a domain one, and the plan record
    /// would say something misleading. Interactive plans are left alone: their token belongs to the user
    /// sitting in front of it and must never be silently swapped.
    /// </summary>
    /// <param name="stepContext">Context the next step would run under</param>
    /// <param name="planId">Only for the log line when minting fails</param>
    private async Task<SkillExecutionContext> RefreshBackgroundTokenAsync(
        SkillExecutionContext stepContext, Guid planId, CancellationToken cancellationToken)
    {
        if (stepContext.TokenRenewalOwnerId is not { } ownerUserId)
        {
            return stepContext;
        }

        var token = await _internalTokenIssuer.IssueForOwnerAsync(ownerUserId, cancellationToken: cancellationToken);
        if (!token.Success)
        {
            _logger.LogWarning(
                "Plan {PlanId} could not renew its token between steps: {Reason}; the remaining steps run " +
                "with the previous one and will fail once it expires",
                planId, token.Reason);
            return stepContext;
        }

        return stepContext with
        {
            AccessToken = token.Token,
            UserPermissions = Permissions.ExpandRoles(token.Roles)
        };
    }

    /// <summary>
    /// Resumes a self-reflection plan under its owner's identity rather than under whoever pressed
    /// resume. The rights come from a freshly minted token carrying the owner's CURRENT roles — the
    /// frozen permission CSV is no longer the source of truth, only the marker that an approved
    /// candidate exists. If no such identity can be established the plan continues under the resuming
    /// caller, which is the behaviour that was already in place for a missing CSV.
    /// </summary>
    /// <param name="plan">The plan being resumed</param>
    /// <param name="goalCandidate">The candidate that produced it, when the origin is self-reflection</param>
    /// <param name="suppliedContext">Context of the caller that triggered the resume</param>
    private async Task<SkillExecutionContext> ResolveResumeExecutionContextAsync(
        AgentPlan plan,
        GoalCandidate? goalCandidate,
        SkillExecutionContext suppliedContext,
        CancellationToken cancellationToken)
    {
        if (plan.Origin != AgentPlanOrigin.SelfReflection)
        {
            return suppliedContext;
        }

        if (goalCandidate?.Status != GoalCandidateStatus.Approved ||
            string.IsNullOrWhiteSpace(goalCandidate.OwnerPermissionsCsv))
        {
            _logger.LogWarning(
                "Plan {PlanId} resumed under origin {Origin} but has no approved goal candidate with frozen " +
                "permissions; continuing under the resuming caller's context instead of the self-reflection identity",
                plan.Id, plan.Origin);
            return suppliedContext;
        }

        if (!Guid.TryParse(goalCandidate.UserId, out var ownerUserId))
        {
            _logger.LogWarning(
                "Plan {PlanId} resumed but goal candidate owner '{UserId}' is not a Guid; continuing under " +
                "the resuming caller's context",
                plan.Id, goalCandidate.UserId);
            return suppliedContext;
        }

        var token = await _internalTokenIssuer.IssueForOwnerAsync(ownerUserId, cancellationToken: cancellationToken);
        if (!token.Success)
        {
            _logger.LogWarning(
                "Plan {PlanId} resumed but no token could be issued for its owner: {Reason}; continuing under " +
                "the resuming caller's context",
                plan.Id, token.Reason);
            return suppliedContext;
        }

        return suppliedContext with
        {
            UserName = GoalSelfReflectionAuditConstants.AuditUserName,
            UserPermissions = Permissions.ExpandRoles(token.Roles),
            AccessToken = token.Token,
            TokenRenewalOwnerId = ownerUserId,
            SessionId = GoalSelfReflectionAuditConstants.SessionIdPrefix + goalCandidate.Id
        };
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

    private async Task FirePlanPausedTriggerAsync(
        AgentPlan plan,
        int stepIndex,
        int totalSteps,
        string skillName,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(plan.UserId, out var userId))
        {
            _logger.LogWarning(
                "Plan {PlanId} paused for approval but UserId {UserId} is missing or not a Guid; skipping proactive trigger",
                plan.Id, plan.UserId);
            return;
        }

        try
        {
            var triggerEvent = new PlanPausedForApprovalTriggerEvent(plan.Id, stepIndex, totalSteps, skillName, userId);
            await _triggerService.OnEventAsync(triggerEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Plan {PlanId} paused-for-approval trigger dispatch failed at step {Index}",
                plan.Id, stepIndex);
        }
    }

    private void TriggerTaskBoundaryCompaction(AgentPlan plan)
    {
        if (plan.SessionId is not { } sessionId)
        {
            return;
        }

        _backgroundTaskService.TriggerConversationCompaction(sessionId.ToString(), TaskBoundaryMinMessages);
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
