// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IScheduleCompletionService
{
    Task<PeriodHoursResource> SaveAndTrackAsync(
        Guid clientId, DateOnly currentDate,
        DateOnly periodStart, DateOnly periodEnd);

    Task<PeriodHoursResource> SaveAndTrackMoveAsync(
        Guid clientId, DateOnly currentDate,
        DateOnly periodStart, DateOnly periodEnd,
        Guid? previousClientId, DateOnly? previousDate);

    Task SaveBulkAndTrackAsync(
        List<(Guid ClientId, DateOnly CurrentDate)> affectedEntries);

    Task SaveAndTrackWithReplaceClientAsync(
        Guid clientId, DateOnly currentDate,
        DateOnly periodStart, DateOnly periodEnd,
        Guid? replaceClientId,
        Guid? previousReplaceClientId = null);
}
