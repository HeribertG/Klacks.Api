using Klacks.Api.Presentation.DTOs.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace Klacks.Api.Infrastructure.Hubs;

public class WorkNotificationService : IWorkNotificationService
{
    private readonly IHubContext<WorkNotificationHub> _hubContext;
    private readonly ILogger<WorkNotificationService> _logger;

    public WorkNotificationService(
        IHubContext<WorkNotificationHub> hubContext,
        ILogger<WorkNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyWorkCreated(WorkNotificationDto notification)
    {
        await SendNotification("WorkCreated", notification);
    }

    public async Task NotifyWorkUpdated(WorkNotificationDto notification)
    {
        await SendNotification("WorkUpdated", notification);
    }

    public async Task NotifyWorkDeleted(WorkNotificationDto notification)
    {
        await SendNotification("WorkDeleted", notification);
    }

    private async Task SendNotification(string method, WorkNotificationDto notification)
    {
        try
        {
            if (string.IsNullOrEmpty(notification.SourceConnectionId))
            {
                await _hubContext.Clients.All.SendAsync(method, notification);
            }
            else
            {
                await _hubContext.Clients
                    .AllExcept(notification.SourceConnectionId)
                    .SendAsync(method, notification);
            }

            _logger.LogDebug("Sent {Method} notification for Work {WorkId}", method, notification.WorkId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending {Method} notification", method);
        }
    }
}
