// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IScheduleCompletionService
{
    Task<PeriodHoursResource> SaveAndTrackAsync(
        Guid clientId, DateOnly currentDate,
        DateOnly periodStart, DateOnly periodEnd,
        Guid? analyseToken);

    Task<PeriodHoursResource> SaveAndTrackMoveAsync(
        Guid clientId, DateOnly currentDate,
        DateOnly periodStart, DateOnly periodEnd,
        Guid? previousClientId, DateOnly? previousDate,
        Guid? analyseToken);

    Task SaveBulkAndTrackAsync(
        List<(Guid ClientId, DateOnly CurrentDate, Guid? AnalyseToken)> affectedEntries);

    Task SaveBulkAndTrackRangeAsync(
        List<(Guid ClientId, DateOnly CurrentDate, Guid? AnalyseToken)> affectedEntries,
        DateOnly periodStart,
        DateOnly periodEnd,
        Guid? bulkAnalyseToken);

    Task SaveAndTrackWithReplaceClientAsync(
        Guid clientId, DateOnly currentDate,
        DateOnly periodStart, DateOnly periodEnd,
        Guid? replaceClientId,
        Guid? analyseToken,
        Guid? previousReplaceClientId = null);
}
