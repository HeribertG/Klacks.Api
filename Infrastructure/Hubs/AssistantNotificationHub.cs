// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Klacks.Api.Infrastructure.Hubs;

[Authorize]
public class AssistantNotificationHub : Hub<IAssistantClient>
{
    private readonly ILogger<AssistantNotificationHub> _logger;
    private readonly IAssistantConnectionTracker _tracker;

    public AssistantNotificationHub(
        ILogger<AssistantNotificationHub> logger,
        IAssistantConnectionTracker tracker)
    {
        _logger = logger;
        _tracker = tracker;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            _tracker.RegisterConnection(userId, Context.ConnectionId);
            _logger.LogInformation("Assistant hub: User {UserId} connected ({ConnectionId})", userId, Context.ConnectionId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _tracker.UnregisterConnection(Context.ConnectionId);
        _logger.LogInformation("Assistant hub: Client disconnected ({ConnectionId})", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public string GetConnectionId()
    {
        return Context.ConnectionId;
    }
}
