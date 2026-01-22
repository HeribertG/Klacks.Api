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

    public async Task NotifyPeriodHoursUpdated(PeriodHoursNotificationDto notification)
    {
        try
        {
            var groupName = WorkNotificationHub.GetScheduleGroupName(notification.StartDate, notification.EndDate);
            var clients = GetGroupClientsExcluding(groupName, notification.SourceConnectionId);

            await clients.SendAsync("PeriodHoursUpdated", notification);

            _logger.LogDebug(
                "Sent PeriodHoursUpdated to group {GroupName} for Client {ClientId}",
                groupName,
                notification.ClientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending PeriodHoursUpdated notification");
        }
    }

    public async Task NotifyPeriodHoursRecalculated(DateOnly startDate, DateOnly endDate)
    {
        try
        {
            var groupName = WorkNotificationHub.GetScheduleGroupName(startDate, endDate);

            await _hubContext.Clients.Group(groupName).SendAsync("PeriodHoursRecalculated", new
            {
                StartDate = startDate,
                EndDate = endDate
            });

            _logger.LogDebug(
                "Sent PeriodHoursRecalculated to group {GroupName}",
                groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending PeriodHoursRecalculated notification");
        }
    }

    private async Task SendNotification(string method, WorkNotificationDto notification)
    {
        try
        {
            var groupName = WorkNotificationHub.GetScheduleGroupName(notification.PeriodStartDate, notification.PeriodEndDate);
            var clients = GetGroupClientsExcluding(groupName, notification.SourceConnectionId);

            await clients.SendAsync(method, notification);

            _logger.LogDebug("Sent {Method} to group {GroupName} for Work {WorkId}", method, groupName, notification.WorkId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending {Method} notification", method);
        }
    }

    private IClientProxy GetGroupClientsExcluding(string groupName, string? excludeConnectionId)
    {
        if (string.IsNullOrEmpty(excludeConnectionId))
        {
            return _hubContext.Clients.Group(groupName);
        }

        return _hubContext.Clients.GroupExcept(groupName, excludeConnectionId);
    }
}
