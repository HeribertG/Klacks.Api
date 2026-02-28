// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Domain.Interfaces.Email;
using Microsoft.AspNetCore.SignalR;

namespace Klacks.Api.Infrastructure.Hubs;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly IHubContext<EmailNotificationHub, IEmailClient> _hubContext;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        IHubContext<EmailNotificationHub, IEmailClient> hubContext,
        ILogger<EmailNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyNewEmailsAsync(int count)
    {
        var notification = new NewEmailsNotificationDto
        {
            Count = count,
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.All.NewEmailsReceived(notification);
        _logger.LogInformation("Notified all clients about {Count} new emails", count);
    }

    public async Task NotifyReadStateChangedAsync(Guid emailId, bool isRead, string folder)
    {
        var notification = new EmailReadStateNotificationDto
        {
            EmailId = emailId,
            IsRead = isRead,
            Folder = folder,
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.All.EmailReadStateChanged(notification);
        _logger.LogDebug("Notified all clients about read state change for email {EmailId}", emailId);
    }
}
