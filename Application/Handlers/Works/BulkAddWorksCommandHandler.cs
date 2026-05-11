// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
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

    public BulkAddWorksCommandHandler(
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IScheduleCompletionService completionService,
        IWorkNotificationFacade notificationFacade,
        IContainerWorkExpansionService expansionService,
        ILogger<BulkAddWorksCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _periodHoursService = periodHoursService;
        _completionService = completionService;
        _notificationFacade = notificationFacade;
        _expansionService = expansionService;
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

                var clientPeriods = new Dictionary<Guid, (DateOnly Start, DateOnly End)>();
                foreach (var work in works)
                {
                    var (start, end) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);
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

                var connectionId = _notificationFacade.GetConnectionId();

                var affectedClients = works.Select(w => w.ClientId).Distinct().ToList();

                foreach (var work in works)
                {
                    var (workStart, workEnd) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);
                    await _notificationFacade.NotifyWorkCreatedAsync(work, connectionId, workStart, workEnd);
                }

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
}
