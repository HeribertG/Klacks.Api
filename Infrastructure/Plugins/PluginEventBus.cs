// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Implementation of IPluginEventBus using SignalR AssistantNotificationHub.
/// Routes plugin events to connected users via the generic PluginEvent method.
/// </summary>

using Klacks.Api.Infrastructure.Hubs;
using Klacks.Plugin.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace Klacks.Api.Infrastructure.Plugins;

public class PluginEventBus : IPluginEventBus
{
    private readonly IHubContext<AssistantNotificationHub, IAssistantClient> _hubContext;
    private readonly IAssistantConnectionTracker _tracker;

    public PluginEventBus(
        IHubContext<AssistantNotificationHub, IAssistantClient> hubContext,
        IAssistantConnectionTracker tracker)
    {
        _hubContext = hubContext;
        _tracker = tracker;
    }

    public async Task PublishToUserAsync(string userId, string eventType, object payload)
    {
        var connectionIds = _tracker.GetConnectionIds(userId).ToList();
        if (connectionIds.Count == 0) return;

        await _hubContext.Clients.Clients(connectionIds).PluginEvent(eventType, payload);
    }

    public async Task BroadcastAsync(string eventType, object payload)
    {
        foreach (var userId in _tracker.GetConnectedUserIds())
        {
            await PublishToUserAsync(userId, eventType, payload);
        }
    }
}
