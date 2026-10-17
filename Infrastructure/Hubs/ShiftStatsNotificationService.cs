// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Klacks.Api.Infrastructure.Hubs;

/// <summary>
/// Broadcasts the per-day booking counter of a shift to every schedule connection except the sender. Deliberately
/// not filtered by group visibility: the payload is only ShiftId, Date, the Engaged count and the AnalyseToken - no
/// shift name, client or employee data. The shift reads GET Shifts/{id}, Shifts/ByIds, CutList, the shift list and
/// the schedule (incl. partial) answer a hidden shift like a missing one, and the AnalyseToken is checked by
/// ScenarioTokenAccessFilter. NOT yet group-checked: RequiredQualifications/{shiftId}, Cuts/Reset/DateRange and the
/// shift writes (PUT/POST Shifts, Cuts/Batch, Cuts/Reset) - until they are, a broadcast hidden ShiftId can be fed into
/// them. Accepted residual: a restricted user can observe that some hidden shift's counter changed. Any field added to
/// ShiftStatsNotificationDto that carries content requires recipient filtering like WorkNotificationService.
/// </summary>
/// <param name="hubContext">SignalR hub context of the schedule hub</param>
/// <param name="logger">Diagnostics logging</param>
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
