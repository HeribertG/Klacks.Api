// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Makes a plan's progress durable and visible. Every status change is persisted and then broadcast via SignalR
/// (best-effort, skipped without a UserId). A pause for approval additionally fires a PlanPausedForApprovalTriggerEvent
/// through IAgentTriggerService so an unattended plan (e.g. run overnight by a background service) still reaches the owning
/// user's proactive inbox, not just connected clients; that call is best-effort too and is skipped with a log when
/// plan.UserId is missing or not a Guid. A completed plan fires a task-boundary conversation compaction with a lower
/// message threshold than the default post-turn trigger, so short plan runs don't compact needlessly; the plan's SessionId
/// (the chat conversation id) must be present or the trigger is silently skipped.
/// </summary>
/// <param name="planRepository">Persists the AgentPlan row after every status change</param>
/// <param name="notificationService">Broadcasts plan status updates via SignalR</param>
/// <param name="triggerService">Fires the proactive PlanPausedForApprovalTriggerEvent</param>
/// <param name="backgroundTaskService">Fires the fire-and-forget task-boundary compaction trigger</param>
/// <param name="logger">The plan executor's logger, so broadcast and trigger warnings keep its category</param>

using Klacks.Api.Application.Services.Assistant.Triggers;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Planning;

internal sealed class PlanStatusPublisher
{
    private const int TaskBoundaryMinMessages = 10;

    private readonly IAgentPlanRepository _planRepository;
    private readonly IAssistantNotificationService _notificationService;
    private readonly IAgentTriggerService _triggerService;
    private readonly ILLMBackgroundTaskService _backgroundTaskService;
    private readonly ILogger _logger;

    public PlanStatusPublisher(
        IAgentPlanRepository planRepository,
        IAssistantNotificationService notificationService,
        IAgentTriggerService triggerService,
        ILLMBackgroundTaskService backgroundTaskService,
        ILogger logger)
    {
        _planRepository = planRepository;
        _notificationService = notificationService;
        _triggerService = triggerService;
        _backgroundTaskService = backgroundTaskService;
        _logger = logger;
    }

    public async Task PersistAndPublishAsync(AgentPlan plan, int totalSteps, CancellationToken cancellationToken)
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

    public async Task FirePlanPausedTriggerAsync(
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

    public void TriggerTaskBoundaryCompaction(AgentPlan plan)
    {
        if (plan.SessionId is not { } sessionId)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(plan.UserId))
        {
            _logger.LogWarning(
                "Plan {PlanId} completed with session {SessionId} but carries no UserId; skipping task-boundary compaction",
                plan.Id, sessionId);
            return;
        }

        _backgroundTaskService.TriggerConversationCompaction(sessionId.ToString(), plan.UserId, TaskBoundaryMinMessages);
    }
}
