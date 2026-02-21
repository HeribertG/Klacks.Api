using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Works;

public class BulkDeleteWorksCommandHandler : BaseHandler, IRequestHandler<BulkDeleteWorksCommand, BulkWorksResponse>
{
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IWorkNotificationService _notificationService;
    private readonly IShiftStatsNotificationService _shiftStatsNotificationService;
    private readonly IShiftScheduleService _shiftScheduleService;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BulkDeleteWorksCommandHandler(
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IWorkNotificationService notificationService,
        IShiftStatsNotificationService shiftStatsNotificationService,
        IShiftScheduleService shiftScheduleService,
        IPeriodHoursService periodHoursService,
        IScheduleCompletionService completionService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<BulkDeleteWorksCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _notificationService = notificationService;
        _shiftStatsNotificationService = shiftStatsNotificationService;
        _shiftScheduleService = shiftScheduleService;
        _periodHoursService = periodHoursService;
        _completionService = completionService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<BulkWorksResponse> Handle(BulkDeleteWorksCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var response = new BulkWorksResponse();
            var affectedShifts = new HashSet<(Guid ShiftId, DateOnly Date)>();
            var affectedClients = new HashSet<Guid>();

            var deletedWorks = new List<Work>();
            foreach (var workId in command.Request.WorkIds)
            {
                var work = await _workRepository.Get(workId);
                if (work == null) continue;
                deletedWorks.Add(work);
                await _workRepository.Delete(workId);
            }

            var affected = deletedWorks.Select(w => (w.ClientId, w.CurrentDate)).ToList();
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
                var connectionId = _httpContextAccessor.HttpContext?.Request
                    .Headers[HttpHeaderNames.SignalRConnectionId].FirstOrDefault() ?? string.Empty;

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

                    var notification = _scheduleMapper.ToWorkNotificationDto(work, ScheduleEventTypes.Deleted, connectionId, start, end);
                    await _notificationService.NotifyWorkDeleted(notification);
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

                            var periodHoursNotification = new Application.DTOs.Notifications.PeriodHoursNotificationDto
                            {
                                ClientId = clientId,
                                StartDate = period.Start,
                                EndDate = period.End,
                                Hours = periodHours.Hours,
                                Surcharges = periodHours.Surcharges,
                                GuaranteedHours = periodHours.GuaranteedHours,
                                SourceConnectionId = connectionId
                            };
                            await _notificationService.NotifyPeriodHoursUpdated(periodHoursNotification);
                        }
                    }
                }

                await SendShiftStatsNotificationsAsync(affectedShifts, connectionId, cancellationToken);
            }

            response.AffectedShifts = affectedShifts
                .Select(x => new ShiftDatePair { ShiftId = x.ShiftId, Date = x.Date.ToDateTime(TimeOnly.MinValue) })
                .ToList();

            return response;
        }, "BulkDeleteWorks", new { Count = command.Request.WorkIds.Count });
    }

    private async Task SendShiftStatsNotificationsAsync(
        HashSet<(Guid ShiftId, DateOnly Date)> affectedShifts,
        string connectionId,
        CancellationToken cancellationToken)
    {
        var shiftDatePairs = affectedShifts.ToList();

        var shiftStats = await _shiftScheduleService.GetShiftSchedulePartialAsync(shiftDatePairs, cancellationToken);

        foreach (var shiftData in shiftStats)
        {
            var shiftNotification = _scheduleMapper.ToShiftStatsNotificationDto(shiftData, connectionId);
            await _shiftStatsNotificationService.NotifyShiftStatsUpdated(shiftNotification);
        }
    }
}
