// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Klacks.Api.Infrastructure.Hubs;

[Authorize]
public class EmailNotificationHub : Hub<IEmailClient>
{
    private readonly ILogger<EmailNotificationHub> _logger;

    public EmailNotificationHub(ILogger<EmailNotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Email hub: Client connected ({ConnectionId})", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Email hub: Client disconnected ({ConnectionId})", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public string GetConnectionId()
    {
        return Context.ConnectionId;
    }
}
