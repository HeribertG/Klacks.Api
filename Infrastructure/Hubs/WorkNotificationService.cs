using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Klacks.Api.Infrastructure.Hubs;

public class WorkNotificationService : IWorkNotificationService
{
    private readonly IHubContext<WorkNotificationHub, IScheduleClient> _hubContext;
    private readonly IConnectionDateRangeTracker _dateRangeTracker;
    private readonly ILogger<WorkNotificationService> _logger;

    public WorkNotificationService(
        IHubContext<WorkNotificationHub, IScheduleClient> hubContext,
        IConnectionDateRangeTracker dateRangeTracker,
        ILogger<WorkNotificationService> logger)
    {
        _hubContext = hubContext;
        _dateRangeTracker = dateRangeTracker;
        _logger = logger;
    }

    public async Task NotifyWorkCreated(WorkNotificationDto notification)
    {
        await SendWorkNotification(notification, (clients, n) => clients.WorkCreated(n), nameof(IScheduleClient.WorkCreated));
    }

    public async Task NotifyWorkUpdated(WorkNotificationDto notification)
    {
        await SendWorkNotification(notification, (clients, n) => clients.WorkUpdated(n), nameof(IScheduleClient.WorkUpdated));
    }

    public async Task NotifyWorkDeleted(WorkNotificationDto notification)
    {
        await SendWorkNotification(notification, (clients, n) => clients.WorkDeleted(n), nameof(IScheduleClient.WorkDeleted));
    }

    public async Task NotifyScheduleUpdated(ScheduleNotificationDto notification)
    {
        try
        {
            var targetDate = notification.CurrentDate;
            var targetConnections = _dateRangeTracker
                .GetConnectionsForDate(targetDate, notification.SourceConnectionId)
                .ToList();

            if (targetConnections.Count == 0)
            {
                _logger.LogDebug(
                    "SignalR SKIP: ScheduleUpdated - no connections have DateRange containing {Date}",
                    targetDate);
                return;
            }

            await _hubContext.Clients.Clients(targetConnections).ScheduleUpdated(notification);

            _logger.LogDebug(
                "Sent ScheduleUpdated to {Count} connections for Client {ClientId}, Date {Date}",
                targetConnections.Count, notification.ClientId, targetDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending ScheduleUpdated notification");
        }
    }

    public async Task NotifyPeriodHoursUpdated(PeriodHoursNotificationDto notification)
    {
        try
        {
            var targetConnections = _dateRangeTracker
                .GetConnectionsForDateRange(notification.StartDate, notification.EndDate, notification.SourceConnectionId)
                .ToList();

            if (targetConnections.Count == 0)
            {
                _logger.LogDebug(
                    "SignalR SKIP: PeriodHoursUpdated - no connections have DateRange overlapping {Start} - {End}",
                    notification.StartDate, notification.EndDate);
                return;
            }

            _logger.LogDebug("SignalR SEND: PeriodHoursUpdated to {Count} connections", targetConnections.Count);

            await _hubContext.Clients.Clients(targetConnections).PeriodHoursUpdated(notification);

            _logger.LogDebug(
                "Sent PeriodHoursUpdated to {Count} connections for Client {ClientId}",
                targetConnections.Count, notification.ClientId);
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
            var targetConnections = _dateRangeTracker
                .GetConnectionsForDateRange(startDate, endDate)
                .ToList();

            if (targetConnections.Count == 0)
            {
                _logger.LogDebug(
                    "SignalR SKIP: PeriodHoursRecalculated - no connections have DateRange overlapping {Start} - {End}",
                    startDate, endDate);
                return;
            }

            var notification = new PeriodHoursRecalculatedDto
            {
                StartDate = startDate,
                EndDate = endDate
            };
            await _hubContext.Clients.Clients(targetConnections).PeriodHoursRecalculated(notification);

            _logger.LogDebug(
                "Sent PeriodHoursRecalculated to {Count} connections for range {Start} - {End}",
                targetConnections.Count, startDate, endDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending PeriodHoursRecalculated notification");
        }
    }

    public async Task NotifyScheduleChangeTracked(ScheduleChangeNotificationDto notification)
    {
        try
        {
            var targetConnections = _dateRangeTracker
                .GetConnectionsForDate(notification.ChangeDate)
                .ToList();

            if (targetConnections.Count == 0)
            {
                _logger.LogDebug(
                    "SignalR SKIP: ScheduleChangeTracked - no connections have DateRange containing {Date}",
                    notification.ChangeDate);
                return;
            }

            await _hubContext.Clients.Clients(targetConnections).ScheduleChangeTracked(notification);

            _logger.LogDebug(
                "Sent ScheduleChangeTracked to {Count} connections for Client {ClientId}, Date {Date}",
                targetConnections.Count, notification.ClientId, notification.ChangeDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending ScheduleChangeTracked notification");
        }
    }

    public async Task NotifyCollisionsDetected(CollisionListNotificationDto notification)
    {
        try
        {
            if (notification.Collisions.Count == 0 && !notification.IsFullRefresh)
            {
                return;
            }

            var dates = notification.Collisions.Select(c => c.Date).Distinct().ToList();

            if (notification.IsFullRefresh || dates.Count == 0)
            {
                await _hubContext.Clients.All.CollisionsDetected(notification);
                _logger.LogDebug("Sent CollisionsDetected (full refresh) to all connections");
                return;
            }

            var targetConnections = new HashSet<string>();
            foreach (var date in dates)
            {
                foreach (var conn in _dateRangeTracker.GetConnectionsForDate(date))
                {
                    targetConnections.Add(conn);
                }
            }

            if (targetConnections.Count == 0)
            {
                return;
            }

            await _hubContext.Clients.Clients(targetConnections.ToList()).CollisionsDetected(notification);
            _logger.LogDebug("Sent CollisionsDetected to {Count} connections", targetConnections.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending CollisionsDetected notification");
        }
    }

    private async Task SendWorkNotification(
        WorkNotificationDto notification,
        Func<IScheduleClient, WorkNotificationDto, Task> sendAction,
        string methodName)
    {
        try
        {
            var targetDate = notification.CurrentDate;
            var targetConnections = _dateRangeTracker
                .GetConnectionsForDate(targetDate, notification.SourceConnectionId)
                .ToList();

            if (targetConnections.Count == 0)
            {
                _logger.LogDebug(
                    "SignalR SKIP: {Method} - no connections have DateRange containing {Date}",
                    methodName, targetDate);
                return;
            }

            _logger.LogDebug("SignalR SEND: {Method} to {Count} connections (DateRange contains {Date:yyyy-MM-dd})", methodName, targetConnections.Count, targetDate);

            _logger.LogInformation(
                "SignalR SEND: {Method} to {Count} connections for date {Date}, ClientId={ClientId}",
                methodName, targetConnections.Count, targetDate, notification.ClientId);

            var clients = _hubContext.Clients.Clients(targetConnections);
            await sendAction(clients, notification);

            _logger.LogInformation("SignalR SENT: {Method} successfully to {Count} connections", methodName, targetConnections.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending {Method} notification", methodName);
        }
    }
}
