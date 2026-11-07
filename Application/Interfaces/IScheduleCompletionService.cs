// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.DTOs.Schedules;
using Klacks.Api.Domain.Models.Schedules;

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

    /// <summary>
    /// Completes a single-Work write the caller has already committed. Queues the timeline check of the Work's
    /// position (and of the previous position when client or date differ) first, then reprocesses the overtime
    /// successors (K3/K4: their prior-hours sums read committed state, so this runs after the commit and before the
    /// period hours), tracks the change and recalculates and pushes the period hours of the Work's client and, when
    /// the client changed, of the previous client. Without a push it also stores the previous owner's row of the pay
    /// period the Work left (new day outside the given period) and the row of the following pay period of each position
    /// whose day plus a week passes its period end (OvertimeSuccessorReach). When any of these steps throws, the Work stays committed and the
    /// write still succeeds: the failure is logged, tracked changes are discarded, a thorough recalculation of the
    /// pay period (plus the following one when successors can reach into it) is queued to repair surcharges and
    /// period hours, the cached period-hours rows of the affected days are dropped and the live period hours are
    /// returned instead, without a PeriodHoursUpdated push.
    /// </summary>
    /// <param name="work">The Work as committed (added, edited, soft-deleted or restored)</param>
    /// <param name="previous">The pre-write state for an edit or move; null for create, delete and restore</param>
    /// <param name="periodStart">First day of the Work's pay period</param>
    /// <param name="periodEnd">Last day of the Work's pay period</param>
    Task<WorkWriteCompletion> CompleteWorkWriteAsync(
        Work work, Work? previous, DateOnly periodStart, DateOnly periodEnd);

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
