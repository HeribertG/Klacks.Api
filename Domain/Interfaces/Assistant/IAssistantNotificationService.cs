// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAssistantNotificationService
{
    Task SendProactiveMessageAsync(string userId, string message, string? conversationId = null);

    Task SendOnboardingPromptAsync(string userId, string message);

    Task BroadcastPluginEventAsync(string eventType, object payload);

    Task SendPlanUpdateAsync(string userId, Guid planId, string status, int currentStepIndex, int totalSteps, string? lastErrorMessage = null);

    bool IsUserConnected(string userId);

    IEnumerable<string> GetConnectedUserIds();
}
