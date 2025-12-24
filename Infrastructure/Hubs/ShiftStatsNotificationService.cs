using Klacks.Api.Presentation.DTOs.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace Klacks.Api.Infrastructure.Hubs;

public class ShiftStatsNotificationService : IShiftStatsNotificationService
{
    private readonly IHubContext<WorkNotificationHub> _hubContext;
    private readonly ILogger<ShiftStatsNotificationService> _logger;

    public ShiftStatsNotificationService(
        IHubContext<WorkNotificationHub> hubContext,
        ILogger<ShiftStatsNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyShiftStatsUpdated(ShiftStatsNotificationDto notification)
    {
        try
        {
            if (string.IsNullOrEmpty(notification.SourceConnectionId))
            {
                await _hubContext.Clients.All.SendAsync("ShiftStatsUpdated", notification);
            }
            else
            {
                await _hubContext.Clients
                    .AllExcept(notification.SourceConnectionId)
                    .SendAsync("ShiftStatsUpdated", notification);
            }

            _logger.LogDebug(
                "Sent ShiftStatsUpdated notification for Shift {ShiftId} on {Date}",
                notification.ShiftId,
                notification.Date);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending ShiftStatsUpdated notification");
        }
    }
}
