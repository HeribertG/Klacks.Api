// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Messaging;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAssistantNotificationService
{
    Task SendProactiveMessageAsync(string userId, string message, string? conversationId = null);

    Task SendOnboardingPromptAsync(string userId, string message);

    Task BroadcastIncomingMessageAsync(IncomingMessageDto message);

    bool IsUserConnected(string userId);

    IEnumerable<string> GetConnectedUserIds();
}
