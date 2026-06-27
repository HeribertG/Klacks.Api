// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.AspNetCore.SignalR;

namespace Klacks.Api.Infrastructure.Hubs;

public class AssistantNotificationService : IAssistantNotificationService
{
    private readonly IHubContext<AssistantNotificationHub, IAssistantClient> _hubContext;
    private readonly IAssistantConnectionTracker _tracker;
    private readonly ILogger<AssistantNotificationService> _logger;

    public AssistantNotificationService(
        IHubContext<AssistantNotificationHub, IAssistantClient> hubContext,
        IAssistantConnectionTracker tracker,
        ILogger<AssistantNotificationService> logger)
    {
        _hubContext = hubContext;
        _tracker = tracker;
        _logger = logger;
    }

    public async Task SendProactiveMessageAsync(string userId, string message, string? conversationId = null, IReadOnlyDictionary<string, string>? contentParams = null)
    {
        var connectionIds = _tracker.GetConnectionIds(userId).ToList();
        if (connectionIds.Count == 0)
        {
            _logger.LogDebug("No connections found for user {UserId}, skipping proactive message", userId);
            return;
        }

        var dto = new ProactiveMessageDto
        {
            MessageId = Guid.NewGuid().ToString(),
            Content = message,
            ConversationId = conversationId,
            Timestamp = DateTime.UtcNow,
            MessageType = "proactive",
            ContentParams = contentParams
        };

        await _hubContext.Clients.Clients(connectionIds).ProactiveMessage(dto);
        _logger.LogInformation("Sent proactive message to user {UserId} ({Count} connections)", userId, connectionIds.Count);
    }

    public async Task SendOnboardingPromptAsync(string userId, string message)
    {
        var connectionIds = _tracker.GetConnectionIds(userId).ToList();
        if (connectionIds.Count == 0)
        {
            _logger.LogDebug("No connections found for user {UserId}, skipping onboarding prompt", userId);
            return;
        }

        var dto = new ProactiveMessageDto
        {
            MessageId = Guid.NewGuid().ToString(),
            Content = message,
            Timestamp = DateTime.UtcNow,
            MessageType = "onboarding"
        };

        await _hubContext.Clients.Clients(connectionIds).OnboardingPrompt(dto);
        _logger.LogInformation("Sent onboarding prompt to user {UserId}", userId);
    }

    public async Task SendPlanUpdateAsync(string userId, Guid planId, string status, int currentStepIndex, int totalSteps, string? lastErrorMessage = null)
    {
        var connectionIds = _tracker.GetConnectionIds(userId).ToList();
        if (connectionIds.Count == 0)
        {
            _logger.LogDebug("No connections found for user {UserId}, skipping plan update {PlanId}", userId, planId);
            return;
        }

        var dto = new AgentPlanUpdateDto
        {
            PlanId = planId,
            Status = status,
            CurrentStepIndex = currentStepIndex,
            TotalSteps = totalSteps,
            LastErrorMessage = lastErrorMessage,
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Clients(connectionIds).PlanUpdated(dto);
        _logger.LogInformation("Sent plan update {PlanId} (status={Status}, step={Step}/{Total}) to user {UserId}",
            planId, status, currentStepIndex, totalSteps, userId);
    }

    public async Task SendEntityChangedAsync(string userId, IReadOnlyList<string> entityTypes, string operation, string skillName)
    {
        var connectionIds = _tracker.GetConnectionIds(userId).ToList();
        if (connectionIds.Count == 0)
        {
            _logger.LogDebug("No connections found for user {UserId}, skipping entity-changed notification", userId);
            return;
        }

        var dto = new EntityChangedDto
        {
            EntityTypes = entityTypes,
            Operation = operation,
            SkillName = skillName,
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Clients(connectionIds).EntityChanged(dto);
        _logger.LogInformation(
            "Sent entity-changed ({Operation} {Entities} via {SkillName}) to user {UserId} ({Count} connections)",
            operation, string.Join(",", entityTypes), skillName, userId, connectionIds.Count);
    }

    public async Task BroadcastPluginEventAsync(string eventType, object payload)
    {
        var connectedUserIds = _tracker.GetConnectedUserIds().ToList();
        if (connectedUserIds.Count == 0)
        {
            _logger.LogDebug("No connected users, skipping plugin event broadcast for {EventType}", eventType);
            return;
        }

        foreach (var userId in connectedUserIds)
        {
            var connectionIds = _tracker.GetConnectionIds(userId).ToList();
            if (connectionIds.Count > 0)
            {
                await _hubContext.Clients.Clients(connectionIds).PluginEvent(eventType, payload);
            }
        }

        _logger.LogInformation("Broadcast plugin event '{EventType}' to {Count} connected user(s)", eventType, connectedUserIds.Count);
    }

    public bool IsUserConnected(string userId)
    {
        return _tracker.IsUserConnected(userId);
    }

    public IEnumerable<string> GetConnectedUserIds()
    {
        return _tracker.GetConnectedUserIds();
    }
}
