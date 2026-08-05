// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Facade for work-related notifications (work, period hours, shift stats, schedule notifications).
/// Bundles IWorkNotificationService, IShiftStatsNotificationService, IShiftScheduleService and ScheduleMapper
/// to reduce DI overload and code duplication in the Works handlers.
/// </summary>
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.DTOs.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IWorkNotificationFacade
{
    string GetConnectionId();

    Task NotifyWorkCreatedAsync(Work work, string connectionId, DateOnly periodStart, DateOnly periodEnd);

    /// <summary>
    /// One event for a batch of created works. The period boundaries differ per date, so the caller
    /// supplies the mapping it already computed for the bulk insert.
    /// </summary>
    /// <param name="works">The created works.</param>
    /// <param name="connectionId">Connection that caused the change.</param>
    /// <param name="periodBoundariesByDate">Period start/end per work date.</param>
    Task NotifyWorksBulkCreatedAsync(
        IReadOnlyList<Work> works,
        string connectionId,
        IReadOnlyDictionary<DateOnly, (DateOnly Start, DateOnly End)> periodBoundariesByDate);

    Task NotifyWorkUpdatedAsync(Work work, string connectionId, DateOnly periodStart, DateOnly periodEnd);

    Task NotifyWorkDeletedAsync(Work work, string connectionId, DateOnly periodStart, DateOnly periodEnd);

    Task NotifyPeriodHoursUpdatedAsync(
        Guid clientId, DateOnly periodStart, DateOnly periodEnd,
        PeriodHoursResource periodHours, string connectionId, Guid? analyseToken);

    Task NotifyShiftStatsAsync(
        HashSet<(Guid ShiftId, DateOnly Date)> affectedShifts,
        string connectionId, Guid? analyseToken, CancellationToken cancellationToken);

    Task NotifyShiftStatsAsync(
        Guid shiftId, DateOnly date,
        string connectionId, Guid? analyseToken, CancellationToken cancellationToken);

    Task NotifyScheduleUpdatedAsync(
        Guid clientId, DateOnly currentDate,
        string connectionId, DateOnly periodStart, DateOnly periodEnd, Guid? analyseToken);
}
