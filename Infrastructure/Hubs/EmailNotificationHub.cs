// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Klacks.Api.Infrastructure.Hubs;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class EmailNotificationHub : Hub<IEmailClient>
{
    private readonly ILogger<EmailNotificationHub> _logger;

    public EmailNotificationHub(ILogger<EmailNotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, SignalRConstants.EmailGroups.User(userId));
            await Groups.AddToGroupAsync(Context.ConnectionId, SignalRConstants.EmailGroups.Subscribers);
        }
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
