// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IScheduleCompletionService
{
    /// <summary>
    /// Commits, tracks the change, queues the timeline check, then recalculates the period hours through
    /// IPeriodHoursService.RecalculateAndNotifyAsync, which also sends the PeriodHoursUpdated push. Callers
    /// therefore do not push the returned period hours a second time.
    /// </summary>
    Task<PeriodHoursResource> SaveAndTrackAsync(
        Guid clientId, DateOnly currentDate,
        DateOnly periodStart, DateOnly periodEnd,
        Guid? analyseToken);

    /// <summary>
    /// Same as SaveAndTrackAsync for a move; recalculates and pushes the period hours of the new owner and,
    /// when the client changed, of the previous owner.
    /// </summary>
    Task<(PeriodHoursResource PeriodHours, PeriodHoursResource? PreviousPeriodHours)> SaveAndTrackMoveAsync(
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
