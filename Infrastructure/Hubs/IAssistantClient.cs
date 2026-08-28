// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Infrastructure.Hubs;

public interface IAssistantClient
{
    Task ProactiveMessage(ProactiveMessageDto message);
    Task ProactiveInboxChanged(ProactiveInboxChangedDto change);
    Task PluginEvent(string eventType, object payload);
    Task PlanUpdated(AgentPlanUpdateDto update);
    Task EntityChanged(EntityChangedDto change);
}
