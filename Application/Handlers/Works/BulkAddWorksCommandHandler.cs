// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates several Works in one call after the day-lock and hard-blocking guards have passed, together with the
/// default expenses of their shifts (same scope, same save). If any Work is for a client outside the caller's
/// group visibility, the whole request is refused exactly like a request for a client that does not exist;
/// nothing is written. Like the grid's locked cells, a Work on a day outside its employee's membership or group period
/// refuses the whole request (unless the command comes from a planner that skips this check); container children follow
/// their parent's cell and are not checked on their own. If any Work is on a shift outside the caller's group visibility,
/// the whole request is refused exactly like a request with a shift that does not exist (one query for all shifts).
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may write for every client of the request</param>
/// <param name="planningPeriodGuard">Refuses a Work whose employee is not plannable on its day (membership or group period)</param>
/// <param name="defaultExpensesApplier">Stages the shifts' default expenses on the new top-level Works before the single bulk save</param>
/// <param name="shiftVisibilityGuard">Names the first shift of the request the calling user may not book</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Services.Shifts;
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
    private const string ClientsNotFoundMessage = "One or more clients of the request were not found";

    private readonly IWorkRepository _workRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IWorkNotificationFacade _notificationFacade;
    private readonly IContainerWorkExpansionService _expansionService;
    private readonly IOvertimeCascadeService _overtimeCascadeService;
    private readonly IDayLockService _dayLockService;
    private readonly IWorkWriteGuard _workWriteGuard;
    private readonly IShiftDefaultExpensesApplier _defaultExpensesApplier;
    private readonly IClientPlanningPeriodGuard _planningPeriodGuard;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;

    public BulkAddWorksCommandHandler(
        IWorkRepository workRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IScheduleCompletionService completionService,
        IWorkNotificationFacade notificationFacade,
        IContainerWorkExpansionService expansionService,
        IOvertimeCascadeService overtimeCascadeService,
        IDayLockService dayLockService,
        IWorkWriteGuard workWriteGuard,
        IShiftDefaultExpensesApplier defaultExpensesApplier,
        IClientPlanningPeriodGuard planningPeriodGuard,
        IShiftVisibilityGuard shiftVisibilityGuard,
        ILogger<BulkAddWorksCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
        _periodHoursService = periodHoursService;
        _completionService = completionService;
        _notificationFacade = notificationFacade;
        _expansionService = expansionService;
        _overtimeCascadeService = overtimeCascadeService;
        _dayLockService = dayLockService;
        _workWriteGuard = workWriteGuard;
        _defaultExpensesApplier = defaultExpensesApplier;
        _planningPeriodGuard = planningPeriodGuard;
        _shiftVisibilityGuard = shiftVisibilityGuard;
    }

    public async Task<BulkWorksResponse> Handle(BulkAddWorksCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            await EnsureClientsAndShiftsVisibleAsync(command.Request.Works, cancellationToken);

            await EnsureDaysWritableAsync(command, cancellationToken);

            var response = new BulkWorksResponse();
            var affectedShifts = new HashSet<(Guid ShiftId, DateOnly Date)>();
            var works = BuildWorks(command.Request.Works, response, affectedShifts);

            await EnsureNoHardBlockingConflictAsync(works, cancellationToken);

            if (works.Count > 0)
            {
                await StageWorksAsync(works, cancellationToken);

                var affected = works.Select(w => (w.ClientId, w.CurrentDate, w.AnalyseToken)).ToList();
                var bulkToken = works.Select(w => w.AnalyseToken).Distinct().Count() == 1 ? works[0].AnalyseToken : null;

                var periodBoundariesByDate = await ResolvePeriodBoundariesByDateAsync(works);
                var clientPeriods = MergeClientPeriods(works, periodBoundariesByDate);

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

                await RecalculatePeriodHoursAsync(works, affectedClients, clientPeriods, connectionId, response);

                await _notificationFacade.NotifyShiftStatsAsync(affectedShifts, connectionId, bulkToken, cancellationToken);
            }

            response.AffectedShifts = affectedShifts
                .Select(x => new ShiftDatePair { ShiftId = x.ShiftId, Date = x.Date.ToDateTime(TimeOnly.MinValue) })
                .ToList();

            return response;
        }, "BulkAddWorks", new { Count = command.Request.Works.Count });
    }

    /// <summary>
    /// Refuses the whole request when one of its days is sealed, and - unless the command comes from a planner that
    /// skips this check - when one of its employees is not plannable on the day.
    /// </summary>
    private async Task EnsureDaysWritableAsync(BulkAddWorksCommand command, CancellationToken cancellationToken)
    {
        await _dayLockService.EnsureNoneLockedAsync(
            command.Request.Works
                .Select(w => (w.CurrentDate, w.ClientId, w.AnalyseToken))
                .Distinct()
                .ToList(),
            cancellationToken);

        if (command.PeriodCheck == PlanningPeriodCheck.Enforce)
        {
            await _planningPeriodGuard.EnsureWithinPeriodsAsync(
                command.Request.Works.Select(w => (w.ClientId, w.CurrentDate)).Distinct().ToList(),
                cancellationToken);
        }
    }

    /// <summary>
    /// Builds the new Works of the request, records their ids and counts in the response and collects the shift
    /// days they touch; an item that cannot be built is logged and counted as failed.
    /// </summary>
    private List<Work> BuildWorks(
        IReadOnlyCollection<BulkWorkItem> items,
        BulkWorksResponse response,
        HashSet<(Guid ShiftId, DateOnly Date)> affectedShifts)
    {
        var works = new List<Work>();

        foreach (var item in items)
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

        return works;
    }

    /// <summary>
    /// Stages the new Works, expands container Works into their children and stages the shifts' default expenses,
    /// all before the single bulk save.
    /// </summary>
    private async Task StageWorksAsync(List<Work> works, CancellationToken cancellationToken)
    {
        foreach (var work in works)
        {
            await _workRepository.Add(work);
        }

        foreach (var work in works)
        {
            await _expansionService.ExpandAsync(work, work.CurrentDate);
        }

        await _defaultExpensesApplier.ApplyAsync(works, cancellationToken);
    }

    /// <summary>
    /// The pay-period boundaries of every distinct Work date, resolved once per date in the order the dates first occur.
    /// </summary>
    private async Task<Dictionary<DateOnly, (DateOnly Start, DateOnly End)>> ResolvePeriodBoundariesByDateAsync(List<Work> works)
    {
        var distinctDates = works.Select(w => w.CurrentDate).Distinct().ToList();
        var periodBoundariesByDate = new Dictionary<DateOnly, (DateOnly Start, DateOnly End)>();
        foreach (var date in distinctDates)
        {
            periodBoundariesByDate[date] = await _periodHoursService.GetPeriodBoundariesAsync(date);
        }

        return periodBoundariesByDate;
    }

    /// <summary>
    /// Per client, the union range from the earliest period start to the latest period end over all its new Works.
    /// </summary>
    private static Dictionary<Guid, (DateOnly Start, DateOnly End)> MergeClientPeriods(
        List<Work> works,
        Dictionary<DateOnly, (DateOnly Start, DateOnly End)> periodBoundariesByDate)
    {
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

        return clientPeriods;
    }

    /// <summary>
    /// Recalculates and broadcasts the period hours of every affected client over its merged range and puts them into
    /// the response; a client's scenario token is passed only when all its new Works share it.
    /// </summary>
    private async Task RecalculatePeriodHoursAsync(
        List<Work> works,
        List<Guid> affectedClients,
        Dictionary<Guid, (DateOnly Start, DateOnly End)> clientPeriods,
        string connectionId,
        BulkWorksResponse response)
    {
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
    }

    /// <summary>
    /// Refuses the whole request like a missing client or shift when any client or shift is outside the caller's group
    /// visibility: one query for the clients, one for the shifts.
    /// </summary>
    private async Task EnsureClientsAndShiftsVisibleAsync(
        IReadOnlyCollection<BulkWorkItem> items, CancellationToken cancellationToken)
    {
        var clientIds = items.Select(w => w.ClientId).Distinct().ToList();
        if (!await _clientVisibilityGuard.AreAllVisibleAsync(clientIds, cancellationToken))
        {
            throw new KeyNotFoundException(ClientsNotFoundMessage);
        }

        var hiddenShiftId = await _shiftVisibilityGuard.FindFirstHiddenAsync(
            items.Select(w => w.ShiftId).Distinct().ToList(), cancellationToken);
        if (hiddenShiftId.HasValue)
        {
            throw new KeyNotFoundException(string.Format(ShiftVisibilityMessages.ShiftNotFound, hiddenShiftId.Value));
        }
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

        var refusal = await _workWriteGuard.FindHardBlockingRefusalAsync(
            realRows, null, WorkWriteOperation.Bulk, cancellationToken);
        if (refusal != null)
        {
            throw WorkWriteConflictException.From(refusal);
        }
    }
}
