using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Constants;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkNotificationService _notificationService;
    private readonly IShiftStatsNotificationService _shiftStatsNotificationService;
    private readonly IShiftScheduleService _shiftScheduleService;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IScheduleChangeTracker _scheduleChangeTracker;

    public BulkAddWorksCommandHandler(
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IWorkNotificationService notificationService,
        IShiftStatsNotificationService shiftStatsNotificationService,
        IShiftScheduleService shiftScheduleService,
        IPeriodHoursService periodHoursService,
        IHttpContextAccessor httpContextAccessor,
        IScheduleChangeTracker scheduleChangeTracker,
        ILogger<BulkAddWorksCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _shiftStatsNotificationService = shiftStatsNotificationService;
        _shiftScheduleService = shiftScheduleService;
        _periodHoursService = periodHoursService;
        _httpContextAccessor = httpContextAccessor;
        _scheduleChangeTracker = scheduleChangeTracker;
    }

    public async Task<BulkWorksResponse> Handle(BulkAddWorksCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var response = new BulkWorksResponse();
            var createdWorks = new List<Work>();
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

                    await _workRepository.Add(work);
                    createdWorks.Add(work);
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

            if (createdWorks.Count > 0)
            {
                await _unitOfWork.CompleteAsync();

                foreach (var work in createdWorks)
                {
                    await _scheduleChangeTracker.TrackChangeAsync(work.ClientId, work.CurrentDate);
                }

                var connectionId = _httpContextAccessor.HttpContext?.Request
                    .Headers[HttpHeaderNames.SignalRConnectionId].FirstOrDefault() ?? string.Empty;

                var affectedClients = createdWorks.Select(w => w.ClientId).Distinct().ToList();
                var periodStart = command.Request.PeriodStart;
                var periodEnd = command.Request.PeriodEnd;

                foreach (var work in createdWorks)
                {
                    var notification = _scheduleMapper.ToWorkNotificationDto(work, ScheduleEventTypes.Created, connectionId, periodStart, periodEnd);
                    await _notificationService.NotifyWorkCreated(notification);
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

                        var periodHoursNotification = new Application.DTOs.Notifications.PeriodHoursNotificationDto
                        {
                            ClientId = clientId,
                            StartDate = periodStart,
                            EndDate = periodEnd,
                            Hours = periodHours.Hours,
                            Surcharges = periodHours.Surcharges,
                            GuaranteedHours = periodHours.GuaranteedHours,
                            SourceConnectionId = connectionId
                        };
                        await _notificationService.NotifyPeriodHoursUpdated(periodHoursNotification);
                    }
                }

                await SendShiftStatsNotificationsAsync(affectedShifts, connectionId, cancellationToken);
            }

            response.AffectedShifts = affectedShifts
                .Select(x => new ShiftDatePair { ShiftId = x.ShiftId, Date = x.Date.ToDateTime(TimeOnly.MinValue) })
                .ToList();

            return response;
        }, "BulkAddWorks", new { Count = command.Request.Works.Count });
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
