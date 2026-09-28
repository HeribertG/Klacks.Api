// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
