using Klacks.Api.Presentation.DTOs.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace Klacks.Api.Infrastructure.Hubs;

public class PeriodHoursNotificationService : IPeriodHoursNotificationService
{
    private readonly IHubContext<PeriodHoursHub> _hubContext;
    private readonly ILogger<PeriodHoursNotificationService> _logger;

    public PeriodHoursNotificationService(
        IHubContext<PeriodHoursHub> hubContext,
        ILogger<PeriodHoursNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyPeriodHoursUpdated(PeriodHoursNotificationDto notification)
    {
        try
        {
            if (string.IsNullOrEmpty(notification.SourceConnectionId))
            {
                await _hubContext.Clients.All.SendAsync("PeriodHoursUpdated", notification);
            }
            else
            {
                await _hubContext.Clients
                    .AllExcept(notification.SourceConnectionId)
                    .SendAsync("PeriodHoursUpdated", notification);
            }

            _logger.LogDebug(
                "Sent PeriodHoursUpdated notification for Client {ClientId}, Period {StartDate}-{EndDate}",
                notification.ClientId,
                notification.StartDate,
                notification.EndDate);
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
            await _hubContext.Clients.All.SendAsync("PeriodHoursRecalculated", new
            {
                StartDate = startDate,
                EndDate = endDate
            });

            _logger.LogDebug(
                "Sent PeriodHoursRecalculated notification for Period {StartDate}-{EndDate}",
                startDate,
                endDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending PeriodHoursRecalculated notification");
        }
    }
}
