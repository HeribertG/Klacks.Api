// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Works;

public class BulkAddWorksCommandHandler : BaseHandler, IRequestHandler<BulkAddWorksCommand, BulkWorksResponse>
{
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IWorkNotificationFacade _notificationFacade;

    public BulkAddWorksCommandHandler(
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IScheduleCompletionService completionService,
        IWorkNotificationFacade notificationFacade,
        ILogger<BulkAddWorksCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _periodHoursService = periodHoursService;
        _completionService = completionService;
        _notificationFacade = notificationFacade;
    }

    public async Task<BulkWorksResponse> Handle(BulkAddWorksCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
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
                        StartTime = item.StartTime,
                        EndTime = item.EndTime,
                        Information = item.Information
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

            if (works.Count > 0)
            {
                foreach (var work in works)
                {
                    await _workRepository.Add(work);
                }

                var affected = works.Select(w => (w.ClientId, w.CurrentDate)).ToList();
                await _completionService.SaveBulkAndTrackAsync(affected);

                var connectionId = _notificationFacade.GetConnectionId();

                var affectedClients = works.Select(w => w.ClientId).Distinct().ToList();
                var periodStart = command.Request.PeriodStart;
                var periodEnd = command.Request.PeriodEnd;

                foreach (var work in works)
                {
                    await _notificationFacade.NotifyWorkCreatedAsync(work, connectionId, periodStart, periodEnd);
                }

                if (affectedClients.Count > 0)
                {
                    response.PeriodHours = new Dictionary<Guid, PeriodHoursResource>();

                    foreach (var clientId in affectedClients)
                    {
                        var periodHours = await _periodHoursService.CalculatePeriodHoursAsync(
                            clientId,
                            periodStart,
                            periodEnd);
                        response.PeriodHours[clientId] = periodHours;

                        await _notificationFacade.NotifyPeriodHoursUpdatedAsync(clientId, periodStart, periodEnd, periodHours, connectionId);
                    }
                }

                await _notificationFacade.NotifyShiftStatsAsync(affectedShifts, connectionId, cancellationToken);
            }

            response.AffectedShifts = affectedShifts
                .Select(x => new ShiftDatePair { ShiftId = x.ShiftId, Date = x.Date.ToDateTime(TimeOnly.MinValue) })
                .ToList();

            return response;
        }, "BulkAddWorks", new { Count = command.Request.Works.Count });
    }
}
