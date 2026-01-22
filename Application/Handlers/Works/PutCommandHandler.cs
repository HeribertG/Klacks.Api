using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Hubs;

namespace Klacks.Api.Application.Handlers.Works;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<WorkResource>, WorkResource?>
{
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IWorkNotificationService _notificationService;
    private readonly IShiftStatsNotificationService _shiftStatsNotificationService;
    private readonly IShiftScheduleService _shiftScheduleService;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PutCommandHandler(
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IWorkNotificationService notificationService,
        IShiftStatsNotificationService shiftStatsNotificationService,
        IShiftScheduleService shiftScheduleService,
        IPeriodHoursService periodHoursService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _notificationService = notificationService;
        _shiftStatsNotificationService = shiftStatsNotificationService;
        _shiftScheduleService = shiftScheduleService;
        _periodHoursService = periodHoursService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<WorkResource?> Handle(PutCommand<WorkResource> request, CancellationToken cancellationToken)
    {
        var existingWork = await _workRepository.Get(request.Resource.Id);
        var oldShiftId = existingWork?.ShiftId;
        var oldDate = existingWork?.CurrentDate;

        var work = _scheduleMapper.ToWorkEntity(request.Resource);

        DateOnly periodStart;
        DateOnly periodEnd;

        if (request.Resource.PeriodStart.HasValue && request.Resource.PeriodEnd.HasValue)
        {
            periodStart = request.Resource.PeriodStart.Value;
            periodEnd = request.Resource.PeriodEnd.Value;
        }
        else
        {
            (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(DateOnly.FromDateTime(work.CurrentDate));
        }

        var (updatedWork, periodHours) = await _workRepository.PutWithPeriodHours(work, periodStart, periodEnd);

        if (updatedWork == null) return null;

        var connectionId = _httpContextAccessor.HttpContext?.Request
            .Headers["X-SignalR-ConnectionId"].FirstOrDefault() ?? string.Empty;
        var notification = _scheduleMapper.ToWorkNotificationDto(updatedWork, "updated", connectionId, periodStart, periodEnd);
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

        var workResource = _scheduleMapper.ToWorkResource(updatedWork);
        workResource.PeriodHours = periodHours;

        return workResource;
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
