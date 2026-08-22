// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Works;

public class BulkAddWorksCommandHandler : BaseHandler, IRequestHandler<BulkAddWorksCommand, BulkWorksResponse>
{
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IWorkNotificationFacade _notificationFacade;
    private readonly IContainerWorkExpansionService _expansionService;
    private readonly IOvertimeCascadeService _overtimeCascadeService;
    private readonly IDayLockService _dayLockService;
    private readonly IPreCommitConflictChecker _conflictChecker;

    public BulkAddWorksCommandHandler(
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IScheduleCompletionService completionService,
        IWorkNotificationFacade notificationFacade,
        IContainerWorkExpansionService expansionService,
        IOvertimeCascadeService overtimeCascadeService,
        IDayLockService dayLockService,
        IPreCommitConflictChecker conflictChecker,
        ILogger<BulkAddWorksCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _periodHoursService = periodHoursService;
        _completionService = completionService;
        _notificationFacade = notificationFacade;
        _expansionService = expansionService;
        _overtimeCascadeService = overtimeCascadeService;
        _dayLockService = dayLockService;
        _conflictChecker = conflictChecker;
    }

    public async Task<BulkWorksResponse> Handle(BulkAddWorksCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            await _dayLockService.EnsureNoneLockedAsync(
                command.Request.Works
                    .Select(w => (w.CurrentDate, w.ClientId, w.AnalyseToken))
                    .Distinct()
                    .ToList(),
                cancellationToken);

            var response = new BulkWorksResponse();
            var works = new List<Work>();
            var affectedShifts = new HashSet<(Guid ShiftId, DateOnly Date)>();

            foreach (var item in command.Request.Works)
            {
                try
                {
                    var work = new Work
                    {
                        Id = Guid.NewGuid(),
                        ShiftId = item.ShiftId,
                        ClientId = item.ClientId,
                        CurrentDate = item.CurrentDate,
                        WorkTime = item.WorkTime,
                        Surcharges = item.Surcharges,
                        StartTime = item.StartTime,
                        EndTime = item.EndTime,
                        Information = item.Information,
                        AnalyseToken = item.AnalyseToken
                    };

                    works.Add(work);
                    response.CreatedIds.Add(work.Id);
                    response.SuccessCount++;
                    affectedShifts.Add((item.ShiftId, item.CurrentDate));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create work for client {ClientId} on {Date}",
                        item.ClientId, item.CurrentDate);
                    response.FailedCount++;
                }
            }

            await EnsureNoHardBlockingConflictAsync(works, cancellationToken);

            if (works.Count > 0)
            {
                foreach (var work in works)
                {
                    await _workRepository.Add(work);
                }

                foreach (var work in works)
                {
                    await _expansionService.ExpandAsync(work, work.CurrentDate);
                }

                var affected = works.Select(w => (w.ClientId, w.CurrentDate, w.AnalyseToken)).ToList();
                var bulkToken = works.Select(w => w.AnalyseToken).Distinct().Count() == 1 ? works[0].AnalyseToken : null;

                var distinctDates = works.Select(w => w.CurrentDate).Distinct().ToList();
                var periodBoundariesByDate = new Dictionary<DateOnly, (DateOnly Start, DateOnly End)>();
                foreach (var date in distinctDates)
                {
                    periodBoundariesByDate[date] = await _periodHoursService.GetPeriodBoundariesAsync(date);
                }

                var clientPeriods = new Dictionary<Guid, (DateOnly Start, DateOnly End)>();
                foreach (var work in works)
                {
                    var (start, end) = periodBoundariesByDate[work.CurrentDate];
                    if (!clientPeriods.TryGetValue(work.ClientId, out var existing))
                    {
                        clientPeriods[work.ClientId] = (start, end);
                    }
                    else
                    {
                        clientPeriods[work.ClientId] = (
                            start < existing.Start ? start : existing.Start,
                            end > existing.End ? end : existing.End);
                    }
                }

                var rangeStart = clientPeriods.Values.Min(p => p.Start);
                var rangeEnd = clientPeriods.Values.Max(p => p.End);

                await _completionService.SaveBulkAndTrackRangeAsync(affected, rangeStart, rangeEnd, bulkToken);

                // K3/K4 cascade: runs after the bulk commit above (successor lookups and prior-hours
                // sums read committed database state), deduplicated to one successor query per client
                // and day and at most one reprocessing per affected Work; the per-client period-hours
                // recalculation below then already includes the cascade's surcharge adjustments.
                await _overtimeCascadeService.ReprocessSuccessorsAsync(works);

                var connectionId = _notificationFacade.GetConnectionId();

                var affectedClients = works.Select(w => w.ClientId).Distinct().ToList();

                await _notificationFacade.NotifyWorksBulkCreatedAsync(works, connectionId, periodBoundariesByDate);

                if (affectedClients.Count > 0)
                {
                    response.PeriodHours = new Dictionary<Guid, PeriodHoursResource>();

                    foreach (var clientId in affectedClients)
                    {
                        var clientTokens = works.Where(w => w.ClientId == clientId).Select(w => w.AnalyseToken).Distinct().ToList();
                        var clientToken = clientTokens.Count == 1 ? clientTokens[0] : null;
                        var (clientStart, clientEnd) = clientPeriods[clientId];

                        var periodHours = await _periodHoursService.RecalculateAndNotifyAsync(
                            clientId,
                            clientStart,
                            clientEnd,
                            clientToken,
                            connectionId);
                        response.PeriodHours[clientId] = periodHours;
                    }
                }

                await _notificationFacade.NotifyShiftStatsAsync(affectedShifts, connectionId, bulkToken, cancellationToken);
            }

            response.AffectedShifts = affectedShifts
                .Select(x => new ShiftDatePair { ShiftId = x.ShiftId, Date = x.Date.ToDateTime(TimeOnly.MinValue) })
                .ToList();

            return response;
        }, "BulkAddWorks", new { Count = command.Request.Works.Count });
    }

    /// <summary>
    /// Refuses the whole batch only when a real-mode row would introduce the one Error class that can
    /// never be reported after the fact instead: a missing mandatory qualification. Bulk planning stays
    /// advisory otherwise, exactly like the single-work path - a compliance rule configured as Block
    /// reports into the error list, and so does a schedule collision (owner decision 2026-08-22): the
    /// batch persists and the async post-commit check surfaces it into the error list rather than
    /// refusing the write. Scenario rows are skipped, mirroring the day-lock contract.
    /// </summary>
    private async Task EnsureNoHardBlockingConflictAsync(
        IReadOnlyList<Work> works,
        CancellationToken cancellationToken)
    {
        var realRows = works
            .Where(w => w.AnalyseToken == null)
            .Select(w => new PlannedWorkRow(w.ClientId, w.CurrentDate, w.StartTime, w.EndTime, w.ShiftId))
            .ToList();

        if (realRows.Count == 0)
        {
            return;
        }

        var conflictCheck = await _conflictChecker.CheckAsync(realRows, null, cancellationToken);
        if (conflictCheck.HasHardBlocking)
        {
            throw new ConflictException(
                "Bulk write blocked: at least one row would introduce " +
                $"{conflictCheck.NewConflicts.Count(c => c.Type == ScheduleValidationType.Error)} " +
                "non-overridable schedule conflict(s). Nothing was committed.");
        }
    }
}
