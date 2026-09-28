// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Application.Interfaces;

public interface IShiftStatsNotificationService
{
    Task NotifyShiftStatsUpdated(ShiftStatsNotificationDto notification);
}
