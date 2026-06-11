// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Klacks.Api.Infrastructure.Hubs;

public class ShiftStatsNotificationService : IShiftStatsNotificationService
{
    private readonly IHubContext<WorkNotificationHub, IScheduleClient> _hubContext;
    private readonly ILogger<ShiftStatsNotificationService> _logger;

    public ShiftStatsNotificationService(
        IHubContext<WorkNotificationHub, IScheduleClient> hubContext,
        ILogger<ShiftStatsNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyShiftStatsUpdated(ShiftStatsNotificationDto notification)
    {
        if (string.IsNullOrEmpty(notification.SourceConnectionId))
        {
            _logger.LogDebug(
                "Skipping ShiftStatsUpdated for Shift {ShiftId} on {Date}: no source connection",
                notification.ShiftId,
                notification.Date);
            return;
        }

        try
        {
            await _hubContext.Clients
                .AllExcept(notification.SourceConnectionId)
                .ShiftStatsUpdated(notification);

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
