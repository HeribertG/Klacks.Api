// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Messaging;
using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Infrastructure.Hubs;

public interface IAssistantClient
{
    Task ProactiveMessage(ProactiveMessageDto message);
    Task OnboardingPrompt(ProactiveMessageDto message);
    Task IncomingMessage(IncomingMessageDto message);
}
