// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAssistantNotificationService
{
    Task SendProactiveMessageAsync(string userId, string message, string? conversationId = null, IReadOnlyDictionary<string, string>? contentParams = null);

    Task SendOnboardingPromptAsync(string userId, string message);

    Task BroadcastPluginEventAsync(string eventType, object payload);

    Task SendPlanUpdateAsync(string userId, Guid planId, string status, int currentStepIndex, int totalSteps, string? lastErrorMessage = null);

    Task SendEntityChangedAsync(string userId, IReadOnlyList<string> entityTypes, string operation, string skillName);

    bool IsUserConnected(string userId);

    IEnumerable<string> GetConnectedUserIds();
}
