// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Works;

public class BulkDeleteWorksCommandHandler : BaseHandler, IRequestHandler<BulkDeleteWorksCommand, BulkWorksResponse>
{
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IWorkNotificationFacade _notificationFacade;

    public BulkDeleteWorksCommandHandler(
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IScheduleCompletionService completionService,
        IWorkNotificationFacade notificationFacade,
        ILogger<BulkDeleteWorksCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _periodHoursService = periodHoursService;
        _completionService = completionService;
        _notificationFacade = notificationFacade;
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

            var deletedWorks = await _workRepository.GetByIdsAsync(command.Request.WorkIds);
            foreach (var work in deletedWorks)
            {
                _workRepository.Remove(work);
            }

            var affected = deletedWorks.Select(w => (w.ClientId, w.CurrentDate, w.AnalyseToken)).ToList();
            await _completionService.SaveBulkAndTrackAsync(affected);

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
                            var periodHours = await _periodHoursService.CalculatePeriodHoursAsync(
                                clientId,
                                period.Start,
                                period.End);
                            response.PeriodHours[clientId] = periodHours;

                            var clientTokens = deletedWorks.Where(w => w.ClientId == clientId).Select(w => w.AnalyseToken).Distinct().ToList();
                            var clientToken = clientTokens.Count == 1 ? clientTokens[0] : null;

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
