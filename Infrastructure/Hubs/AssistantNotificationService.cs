using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Domain.Interfaces.AI;
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

    public async Task SendProactiveMessageAsync(string userId, string message, string? conversationId = null)
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
            MessageType = "proactive"
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

    public bool IsUserConnected(string userId)
    {
        return _tracker.IsUserConnected(userId);
    }

    public IEnumerable<string> GetConnectedUserIds()
    {
        return _tracker.GetConnectedUserIds();
    }
}
