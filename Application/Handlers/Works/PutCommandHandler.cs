using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Hubs;
using Klacks.Api.Infrastructure.Services;

namespace Klacks.Api.Application.Handlers.Works;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<WorkResource>, WorkResource?>
{
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IWorkNotificationService _notificationService;
    private readonly IShiftStatsNotificationService _shiftStatsNotificationService;
    private readonly IShiftScheduleService _shiftScheduleService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly PeriodHoursBackgroundService _periodHoursBackgroundService;

    public PutCommandHandler(
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IWorkNotificationService notificationService,
        IShiftStatsNotificationService shiftStatsNotificationService,
        IShiftScheduleService shiftScheduleService,
        IHttpContextAccessor httpContextAccessor,
        PeriodHoursBackgroundService periodHoursBackgroundService,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _notificationService = notificationService;
        _shiftStatsNotificationService = shiftStatsNotificationService;
        _shiftScheduleService = shiftScheduleService;
        _httpContextAccessor = httpContextAccessor;
        _periodHoursBackgroundService = periodHoursBackgroundService;
    }

    public async Task<WorkResource?> Handle(PutCommand<WorkResource> request, CancellationToken cancellationToken)
    {
        var existingWork = await _workRepository.Get(request.Resource.Id);
        var oldShiftId = existingWork?.ShiftId;
        var oldDate = existingWork?.CurrentDate;

        var work = _scheduleMapper.ToWorkEntity(request.Resource);
        var updatedWork = await _workRepository.Put(work);

        if (updatedWork == null) return null;

        var connectionId = _httpContextAccessor.HttpContext?.Request
            .Headers["X-SignalR-ConnectionId"].FirstOrDefault() ?? string.Empty;

        var notification = _scheduleMapper.ToWorkNotificationDto(updatedWork, "updated", connectionId);
        await _notificationService.NotifyWorkUpdated(notification);

        var affectedShifts = new HashSet<(Guid ShiftId, DateTime Date)>
        {
            (updatedWork.ShiftId, updatedWork.CurrentDate)
        };

        if (oldShiftId.HasValue && oldDate.HasValue &&
            (oldShiftId.Value != updatedWork.ShiftId || oldDate.Value.Date != updatedWork.CurrentDate.Date))
        {
            affectedShifts.Add((oldShiftId.Value, oldDate.Value));
        }

        await SendShiftStatsNotificationsAsync(affectedShifts, connectionId, cancellationToken);

        _periodHoursBackgroundService.QueueRecalculation(
            updatedWork.ClientId,
            DateOnly.FromDateTime(updatedWork.CurrentDate));

        return _scheduleMapper.ToWorkResource(updatedWork);
    }

    private async Task SendShiftStatsNotificationsAsync(
        HashSet<(Guid ShiftId, DateTime Date)> affectedShifts,
        string connectionId,
        CancellationToken cancellationToken)
    {
        var shiftDatePairs = affectedShifts
            .Select(x => (x.ShiftId, DateOnly.FromDateTime(x.Date)))
            .ToList();

        var shiftStats = await _shiftScheduleService.GetShiftSchedulePartialAsync(shiftDatePairs, cancellationToken);

        foreach (var shiftData in shiftStats)
        {
            var shiftNotification = _scheduleMapper.ToShiftStatsNotificationDto(shiftData, connectionId);
            await _shiftStatsNotificationService.NotifyShiftStatsUpdated(shiftNotification);
        }
    }
}
