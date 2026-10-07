// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Soft-deletes several Works in one call and recalculates the affected period hours. Works owned by
/// clients outside the caller's group visibility are treated exactly like ids that do not exist: they are
/// not deleted and count as failed. Like the single delete, the whole request is refused when any visible work
/// lies on a sealed day (IDayLockService), whatever the caller's role.
/// </summary>
/// <param name="clientVisibilityGuard">Filters the Works down to clients the calling user may write for</param>
/// <param name="dayLockService">Refuses deletions on days sealed globally or for a group of the client</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Works;

public class BulkDeleteWorksCommandHandler : BaseHandler, IRequestHandler<BulkDeleteWorksCommand, BulkWorksResponse>
{
    private readonly IWorkRepository _workRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IWorkNotificationFacade _notificationFacade;
    private readonly IOvertimeCascadeService _overtimeCascadeService;
    private readonly IDayLockService _dayLockService;

    public BulkDeleteWorksCommandHandler(
        IWorkRepository workRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IScheduleCompletionService completionService,
        IWorkNotificationFacade notificationFacade,
        IOvertimeCascadeService overtimeCascadeService,
        IDayLockService dayLockService,
        ILogger<BulkDeleteWorksCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
        _periodHoursService = periodHoursService;
        _completionService = completionService;
        _notificationFacade = notificationFacade;
        _overtimeCascadeService = overtimeCascadeService;
        _dayLockService = dayLockService;
    }

    public async Task<BulkWorksResponse> Handle(BulkDeleteWorksCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (command.Request.WorkIds.Count > RateLimitingPolicies.MaxBulkOperationItems)
                throw new ArgumentException($"Maximum {RateLimitingPolicies.MaxBulkOperationItems} items allowed per bulk operation.");

            var response = new BulkWorksResponse();
            var affectedShifts = new HashSet<(Guid ShiftId, DateOnly Date)>();
            var affectedClients = new HashSet<Guid>();

            var foundWorks = await _workRepository.GetByIdsAsync(command.Request.WorkIds);
            var deletedWorks = await _clientVisibilityGuard.FilterVisibleAsync(
                foundWorks, w => w.ClientId, cancellationToken);
            await _dayLockService.EnsureNoneLockedAsync(
                deletedWorks.Select(w => (w.CurrentDate, w.ClientId, w.AnalyseToken)).ToList(),
                cancellationToken);

            foreach (var work in deletedWorks)
            {
                _workRepository.Remove(work);
            }

            var affected = deletedWorks.Select(w => (w.ClientId, w.CurrentDate, w.AnalyseToken)).ToList();
            await _completionService.SaveBulkAndTrackAsync(affected);

            // K3/K4 cascade: runs after the bulk soft-delete commit above; the detached entities still
            // carry the client/date/start time that identify each period and ordering position, and the
            // per-client period-hours calculation below then already includes the cascade's adjustments.
            await _overtimeCascadeService.ReprocessSuccessorsAsync(deletedWorks);

            foreach (var work in deletedWorks)
            {
                affectedShifts.Add((work.ShiftId, work.CurrentDate));
                affectedClients.Add(work.ClientId);
                response.DeletedIds.Add(work.Id);
                response.SuccessCount++;
            }

            response.FailedCount = command.Request.WorkIds.Count - deletedWorks.Count;

            if (deletedWorks.Count > 0)
            {
                var connectionId = _notificationFacade.GetConnectionId();

                var clientPeriods = new Dictionary<Guid, (DateOnly Start, DateOnly End)>();

                foreach (var work in deletedWorks)
                {
                    var (start, end) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);

                    if (!clientPeriods.ContainsKey(work.ClientId))
                    {
                        clientPeriods[work.ClientId] = (start, end);
                    }
                    else
                    {
                        var existing = clientPeriods[work.ClientId];
                        clientPeriods[work.ClientId] = (
                            start < existing.Start ? start : existing.Start,
                            end > existing.End ? end : existing.End
                        );
                    }

                    await _notificationFacade.NotifyWorkDeletedAsync(work, connectionId, start, end);
                }

                if (affectedClients.Count > 0)
                {
                    response.PeriodHours = new Dictionary<Guid, PeriodHoursResource>();

                    foreach (var clientId in affectedClients)
                    {
                        if (clientPeriods.TryGetValue(clientId, out var period))
                        {
                            var clientTokens = deletedWorks.Where(w => w.ClientId == clientId).Select(w => w.AnalyseToken).Distinct().ToList();
                            var clientToken = clientTokens.Count == 1 ? clientTokens[0] : null;

                            var periodHours = await _periodHoursService.CalculatePeriodHoursAsync(
                                clientId,
                                period.Start,
                                period.End,
                                clientToken);
                            response.PeriodHours[clientId] = periodHours;

                            await _notificationFacade.NotifyPeriodHoursUpdatedAsync(clientId, period.Start, period.End, periodHours, connectionId, clientToken);
                        }
                    }
                }

                var bulkToken = deletedWorks.Select(w => w.AnalyseToken).Distinct().Count() == 1 ? deletedWorks[0].AnalyseToken : null;
                await _notificationFacade.NotifyShiftStatsAsync(affectedShifts, connectionId, bulkToken, cancellationToken);
            }

            response.AffectedShifts = affectedShifts
                .Select(x => new ShiftDatePair { ShiftId = x.ShiftId, Date = x.Date.ToDateTime(TimeOnly.MinValue) })
                .ToList();

            return response;
        }, "BulkDeleteWorks", new { Count = command.Request.WorkIds.Count });
    }
}
