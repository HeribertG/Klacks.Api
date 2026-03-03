// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Application.Interfaces;

public interface IShiftStatsNotificationService
{
    Task NotifyShiftStatsUpdated(ShiftStatsNotificationDto notification);
}
