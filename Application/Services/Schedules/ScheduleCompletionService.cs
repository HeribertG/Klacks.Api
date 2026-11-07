// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Post-commit completion of schedule writes: change markers, timeline checks, overtime successors and the period-hours
/// recalculation with its PeriodHoursUpdated push. For a single-Work write (CompleteWorkWriteAsync) a failure after the
/// commit does not fail the write: it queues a thorough recalculation, drops the affected cached period-hours rows and
/// answers with the live period hours.
/// </summary>
/// <param name="overtimeCascadeService">Reprocesses the overtime successors of a committed single-Work write</param>
/// <param name="thoroughRecalculationQueue">Repairs surcharges and period hours when a single-Work completion fails</param>
/// <param name="logger">Records a failed single-Work completion and every repair step that fails as well</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public class ScheduleCompletionService : IScheduleCompletionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IScheduleChangeTracker _scheduleChangeTracker;
    private readonly IScheduleTimelineService _timelineService;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOvertimeCascadeService _overtimeCascadeService;
    private readonly IThoroughRecalculationQueue _thoroughRecalculationQueue;
    private readonly ILogger<ScheduleCompletionService> _logger;

    public ScheduleCompletionService(
        IUnitOfWork unitOfWork,
        IScheduleChangeTracker scheduleChangeTracker,
        IScheduleTimelineService timelineService,
        IPeriodHoursService periodHoursService,
        IHttpContextAccessor httpContextAccessor,
        IOvertimeCascadeService overtimeCascadeService,
        IThoroughRecalculationQueue thoroughRecalculationQueue,
        ILogger<ScheduleCompletionService> logger)
    {
        _unitOfWork = unitOfWork;
        _scheduleChangeTracker = scheduleChangeTracker;
        _timelineService = timelineService;
        _periodHoursService = periodHoursService;
        _httpContextAccessor = httpContextAccessor;
        _overtimeCascadeService = overtimeCascadeService;
        _thoroughRecalculationQueue = thoroughRecalculationQueue;
        _logger = logger;
    }

    public async Task<PeriodHoursResource> SaveAndTrackAsync(
        Guid clientId, DateOnly currentDate,
        DateOnly periodStart, DateOnly periodEnd,
        Guid? analyseToken)
    {
        await _unitOfWork.CompleteAsync();
        await _scheduleChangeTracker.TrackChangeAsync(clientId, currentDate, analyseToken);
        _timelineService.QueueCheck(clientId, currentDate, analyseToken);
        return await RecalculateAndGetPeriodHoursAsync(clientId, periodStart, periodEnd, analyseToken);
    }

    public async Task<(PeriodHoursResource PeriodHours, PeriodHoursResource? PreviousPeriodHours)> SaveAndTrackMoveAsync(
        Guid clientId, DateOnly currentDate,
        DateOnly periodStart, DateOnly periodEnd,
        Guid? previousClientId, DateOnly? previousDate,
        Guid? analyseToken)
    {
        await _unitOfWork.CompleteAsync();
        await _scheduleChangeTracker.TrackChangeAsync(clientId, currentDate, analyseToken);
        _timelineService.QueueCheck(clientId, currentDate, analyseToken);

        if (previousClientId.HasValue && previousDate.HasValue &&
            (previousClientId.Value != clientId || previousDate.Value != currentDate))
        {
            await _scheduleChangeTracker.TrackChangeAsync(previousClientId.Value, previousDate.Value, analyseToken);
            _timelineService.QueueCheck(previousClientId.Value, previousDate.Value, analyseToken);
        }

        var periodHours = await RecalculateAndGetPeriodHoursAsync(clientId, periodStart, periodEnd, analyseToken);

        // Recalculated separately only when the CLIENT changed. The only caller (AbsenceCoverMaterializer) moves a
        // work to another person on the same day, so the previous owner's row lies in the same pay period.
        PeriodHoursResource? previousPeriodHours = previousClientId.HasValue && previousClientId.Value != clientId
            ? await RecalculateAndGetPeriodHoursAsync(previousClientId.Value, periodStart, periodEnd, analyseToken)
            : null;

        return (periodHours, previousPeriodHours);
    }

    public async Task<WorkWriteCompletion> CompleteWorkWriteAsync(
        Work work, Work? previous, DateOnly periodStart, DateOnly periodEnd)
    {
        var previousPosition = previous != null
            && (previous.ClientId != work.ClientId || previous.CurrentDate != work.CurrentDate)
                ? previous
                : null;
        Guid? previousClientId = previous != null && previous.ClientId != work.ClientId ? previous.ClientId : null;

        _timelineService.QueueCheck(work.ClientId, work.CurrentDate, work.AnalyseToken);
        if (previousPosition != null)
        {
            _timelineService.QueueCheck(previousPosition.ClientId, previousPosition.CurrentDate, work.AnalyseToken);
        }

        try
        {
            await _overtimeCascadeService.ReprocessSuccessorsAsync(work, previous);

            await _scheduleChangeTracker.TrackChangeAsync(work.ClientId, work.CurrentDate, work.AnalyseToken);
            if (previousPosition != null)
            {
                await _scheduleChangeTracker.TrackChangeAsync(
                    previousPosition.ClientId, previousPosition.CurrentDate, work.AnalyseToken);
            }

            var periodHours = await RecalculateAndGetPeriodHoursAsync(
                work.ClientId, periodStart, periodEnd, work.AnalyseToken);
            var previousPeriodHours = previousClientId.HasValue
                ? await RecalculateAndGetPeriodHoursAsync(previousClientId.Value, periodStart, periodEnd, work.AnalyseToken)
                : null;
            await RecalculateOtherPeriodsAsync(work, previousPosition, previousClientId, periodStart, periodEnd);

            return new WorkWriteCompletion(periodHours, previousPeriodHours);
        }
        catch (Exception exception)
        {
            return await RecoverFromFailedCompletionAsync(
                exception, work, previousPosition, previousClientId, periodStart, periodEnd);
        }
    }

    /// <summary>
    /// Stores, without a push, the rows of the pay periods a single-Work write reaches beyond the Work's own period: the
    /// previous owner's row of the period the Work left when it moved to a day outside its new period (that row would
    /// otherwise keep the moved Work's hours), and the row of the following period of every position whose day plus a
    /// week passes its period end (OvertimeSuccessorReach: the cascade changed the surcharges of successors there). No
    /// push, because the Ui writes a pushed value into the hours of its visible period whatever period it belongs to.
    /// A period already recalculated for that person is not recalculated again.
    /// </summary>
    /// <param name="work">The Work as committed</param>
    /// <param name="previousPosition">The pre-write state when client or date changed, otherwise null</param>
    /// <param name="previousClientId">The previous client when the write moved the Work to another client, otherwise null</param>
    /// <param name="periodStart">First day of the Work's new pay period</param>
    /// <param name="periodEnd">Last day of the Work's new pay period</param>
    private async Task RecalculateOtherPeriodsAsync(
        Work work, Work? previousPosition, Guid? previousClientId, DateOnly periodStart, DateOnly periodEnd)
    {
        var recalculated = new HashSet<(Guid ClientId, DateOnly Start)> { (work.ClientId, periodStart) };
        if (previousClientId.HasValue)
        {
            recalculated.Add((previousClientId.Value, periodStart));
        }

        var positions = new List<(Guid ClientId, DateOnly Day, DateOnly PeriodEnd)> { (work.ClientId, work.CurrentDate, periodEnd) };
        if (previousPosition != null)
        {
            (DateOnly Start, DateOnly End) leftPeriod = previousPosition.CurrentDate >= periodStart && previousPosition.CurrentDate <= periodEnd
                ? (periodStart, periodEnd)
                : await _periodHoursService.GetPeriodBoundariesAsync(previousPosition.CurrentDate);
            await StoreOnceAsync(previousPosition.ClientId, leftPeriod, work.AnalyseToken, recalculated);
            positions.Add((previousPosition.ClientId, previousPosition.CurrentDate, leftPeriod.End));
        }

        foreach (var (clientId, day, ownPeriodEnd) in positions)
        {
            if (OvertimeSuccessorReach.ReachesFollowingPeriod(day, ownPeriodEnd))
            {
                var following = await _periodHoursService.GetPeriodBoundariesAsync(ownPeriodEnd.AddDays(1));
                await StoreOnceAsync(clientId, following, work.AnalyseToken, recalculated);
            }
        }
    }

    private async Task StoreOnceAsync(
        Guid clientId,
        (DateOnly Start, DateOnly End) period,
        Guid? analyseToken,
        HashSet<(Guid ClientId, DateOnly Start)> recalculated)
    {
        if (recalculated.Add((clientId, period.Start)))
        {
            await _periodHoursService.RecalculatePeriodHoursAsync(clientId, period.Start, period.End, analyseToken);
        }
    }

    public async Task SaveBulkAndTrackAsync(
        List<(Guid ClientId, DateOnly CurrentDate, Guid? AnalyseToken)> affectedEntries)
    {
        await _unitOfWork.CompleteAsync();

        foreach (var (clientId, currentDate, analyseToken) in affectedEntries)
        {
            await _scheduleChangeTracker.TrackChangeAsync(clientId, currentDate, analyseToken);
            _timelineService.QueueCheck(clientId, currentDate, analyseToken);
        }
    }

    public async Task SaveBulkAndTrackRangeAsync(
        List<(Guid ClientId, DateOnly CurrentDate, Guid? AnalyseToken)> affectedEntries,
        DateOnly periodStart,
        DateOnly periodEnd,
        Guid? bulkAnalyseToken)
    {
        await _unitOfWork.CompleteAsync();

        foreach (var (clientId, currentDate, analyseToken) in affectedEntries)
        {
            await _scheduleChangeTracker.TrackChangeAsync(clientId, currentDate, analyseToken);
        }

        var distinctTokens = affectedEntries
            .Select(e => e.AnalyseToken)
            .Distinct()
            .ToList();

        if (distinctTokens.Count <= 1)
        {
            _timelineService.QueueRangeCheck(periodStart, periodEnd, bulkAnalyseToken);
        }
        else
        {
            foreach (var token in distinctTokens)
            {
                _timelineService.QueueRangeCheck(periodStart, periodEnd, token);
            }
        }
    }

    public async Task SaveAndTrackWithReplaceClientAsync(
        Guid clientId, DateOnly currentDate,
        DateOnly periodStart, DateOnly periodEnd,
        Guid? replaceClientId,
        Guid? analyseToken,
        Guid? previousReplaceClientId = null)
    {
        await _unitOfWork.CompleteAsync();

        await _scheduleChangeTracker.TrackChangeAsync(clientId, currentDate, analyseToken);
        _timelineService.QueueCheck(clientId, currentDate, analyseToken);
        await RecalculateAndGetPeriodHoursAsync(clientId, periodStart, periodEnd, analyseToken);

        if (replaceClientId.HasValue)
        {
            await _scheduleChangeTracker.TrackChangeAsync(replaceClientId.Value, currentDate, analyseToken);
            _timelineService.QueueCheck(replaceClientId.Value, currentDate, analyseToken);
            await RecalculateAndGetPeriodHoursAsync(replaceClientId.Value, periodStart, periodEnd, analyseToken);
        }

        if (previousReplaceClientId.HasValue && previousReplaceClientId != replaceClientId)
        {
            await _scheduleChangeTracker.TrackChangeAsync(previousReplaceClientId.Value, currentDate, analyseToken);
            _timelineService.QueueCheck(previousReplaceClientId.Value, currentDate, analyseToken);
            await RecalculateAndGetPeriodHoursAsync(previousReplaceClientId.Value, periodStart, periodEnd, analyseToken);
        }
    }

    private async Task<PeriodHoursResource> RecalculateAndGetPeriodHoursAsync(
        Guid clientId, DateOnly periodStart, DateOnly periodEnd, Guid? analyseToken)
    {
        var connectionId = _httpContextAccessor.HttpContext?.Request
            .Headers[HttpHeaderNames.SignalRConnectionId].FirstOrDefault();

        return await _periodHoursService.RecalculateAndNotifyAsync(
            clientId, periodStart, periodEnd, analyseToken, connectionId);
    }

    /// <summary>
    /// Repairs what a failed single-Work completion left behind without failing the committed write. The tracked
    /// changes are discarded first, so no later save flushes a half-staged step. Every queued window is a real pay
    /// period (the following one first), because the window is also the key of the cached period-hours row the payroll
    /// export reads. Every step that can reach the database is guarded on
    /// its own and only logged when it fails.
    /// </summary>
    /// <param name="previousPosition">The pre-write state when client or date changed, otherwise null</param>
    /// <param name="previousClientId">The previous client when the write moved the Work to another client, otherwise null</param>
    private async Task<WorkWriteCompletion> RecoverFromFailedCompletionAsync(
        Exception exception,
        Work work,
        Work? previousPosition,
        Guid? previousClientId,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        _logger.LogError(
            exception,
            "Completing the write of Work {WorkId} (client {ClientId}, scenario {AnalyseToken}) failed after its commit. The Work is committed; the queued thorough recalculation repairs the successors' surcharges and the period hours",
            work.Id,
            work.ClientId,
            work.AnalyseToken);

        _unitOfWork.DiscardTrackedChanges();

        Guid[] clientIds = previousClientId.HasValue ? [work.ClientId, previousClientId.Value] : [work.ClientId];

        if (OvertimeSuccessorReach.ReachesFollowingPeriod(work.CurrentDate, periodEnd))
        {
            await QueueFollowingPeriodAsync(work, periodEnd, clientIds);
        }

        QueueThoroughRecalculation(work, periodStart, periodEnd, clientIds);

        await InvalidateCachedPeriodHoursAsync(work, work.ClientId, work.CurrentDate);
        if (previousPosition != null)
        {
            await InvalidateCachedPeriodHoursAsync(work, previousPosition.ClientId, previousPosition.CurrentDate);
        }

        var periodHours = await CalculateLivePeriodHoursAsync(work, work.ClientId, periodStart, periodEnd);
        var previousPeriodHours = previousClientId.HasValue
            ? await CalculateLivePeriodHoursAsync(work, previousClientId.Value, periodStart, periodEnd)
            : null;

        return new WorkWriteCompletion(periodHours, previousPeriodHours);
    }

    private async Task QueueFollowingPeriodAsync(Work work, DateOnly periodEnd, Guid[] clientIds)
    {
        try
        {
            var (followingStart, followingEnd) = await _periodHoursService.GetPeriodBoundariesAsync(periodEnd.AddDays(1));
            QueueThoroughRecalculation(work, followingStart, followingEnd, clientIds);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "The pay period after {PeriodEnd} could not be resolved for Work {WorkId} (client {ClientId}, scenario {AnalyseToken}); the surcharges of its successors in that period stay stale until the next recalculation",
                periodEnd,
                work.Id,
                work.ClientId,
                work.AnalyseToken);
        }
    }

    private void QueueThoroughRecalculation(Work work, DateOnly startDate, DateOnly endDate, Guid[] clientIds)
    {
        if (_thoroughRecalculationQueue.QueueRecalculation(startDate, endDate, null, work.AnalyseToken, clientIds))
        {
            return;
        }

        _logger.LogError(
            "The thorough recalculation of {StartDate} to {EndDate} for Work {WorkId} (client {ClientId}, scenario {AnalyseToken}) was rejected; surcharges and period hours stay stale until the next recalculation",
            startDate,
            endDate,
            work.Id,
            work.ClientId,
            work.AnalyseToken);
    }

    private async Task InvalidateCachedPeriodHoursAsync(Work work, Guid clientId, DateOnly date)
    {
        try
        {
            await _periodHoursService.InvalidateCacheAsync(clientId, date, work.AnalyseToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "The cached period hours of client {ClientId} on {Date} (scenario {AnalyseToken}) could not be dropped after the failed completion of Work {WorkId}; they stay stale until the queued recalculation",
                clientId,
                date,
                work.AnalyseToken,
                work.Id);
        }
    }

    private async Task<PeriodHoursResource?> CalculateLivePeriodHoursAsync(
        Work work, Guid clientId, DateOnly periodStart, DateOnly periodEnd)
    {
        try
        {
            return await _periodHoursService.CalculatePeriodHoursAsync(clientId, periodStart, periodEnd, work.AnalyseToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "The live period hours of client {ClientId} from {PeriodStart} to {PeriodEnd} (scenario {AnalyseToken}) could not be read after the failed completion of Work {WorkId}; the write answers without them",
                clientId,
                periodStart,
                periodEnd,
                work.AnalyseToken,
                work.Id);
            return null;
        }
    }
}
