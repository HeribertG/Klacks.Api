// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Facade fuer Work-bezogene Notifications (Work-, PeriodHours-, ShiftStats-, Schedule-Notifications).
/// Buendelt IWorkNotificationService, IShiftStatsNotificationService, IShiftScheduleService und ScheduleMapper,
/// um DI-Overload und Code-Duplikation in den Works-Handlern zu reduzieren.
/// </summary>
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.DTOs.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IWorkNotificationFacade
{
    string GetConnectionId();

    Task NotifyWorkCreatedAsync(Work work, string connectionId, DateOnly periodStart, DateOnly periodEnd);

    Task NotifyWorkUpdatedAsync(Work work, string connectionId, DateOnly periodStart, DateOnly periodEnd);

    Task NotifyWorkDeletedAsync(Work work, string connectionId, DateOnly periodStart, DateOnly periodEnd);

    Task NotifyPeriodHoursUpdatedAsync(
        Guid clientId, DateOnly periodStart, DateOnly periodEnd,
        PeriodHoursResource periodHours, string connectionId);

    Task NotifyShiftStatsAsync(
        HashSet<(Guid ShiftId, DateOnly Date)> affectedShifts,
        string connectionId, CancellationToken cancellationToken);

    Task NotifyShiftStatsAsync(
        Guid shiftId, DateOnly date,
        string connectionId, CancellationToken cancellationToken);

    Task NotifyScheduleUpdatedAsync(
        Guid clientId, DateOnly currentDate,
        string connectionId, DateOnly periodStart, DateOnly periodEnd);
}
