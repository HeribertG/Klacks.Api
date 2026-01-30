using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Hubs;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Works;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<WorkResource>, WorkResource?>
{
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IWorkNotificationService _notificationService;
    private readonly IShiftStatsNotificationService _shiftStatsNotificationService;
    private readonly IShiftScheduleService _shiftScheduleService;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PutCommandHandler(
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IWorkNotificationService notificationService,
        IShiftStatsNotificationService shiftStatsNotificationService,
        IShiftScheduleService shiftScheduleService,
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
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
        _scheduleEntriesService = scheduleEntriesService;
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
            (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);
        }

        var (updatedWork, periodHours) = await _workRepository.PutWithPeriodHours(work, periodStart, periodEnd);

        if (updatedWork == null) return null;

        var connectionId = _httpContextAccessor.HttpContext?.Request
            .Headers["X-SignalR-ConnectionId"].FirstOrDefault() ?? string.Empty;
        var notification = _scheduleMapper.ToWorkNotificationDto(updatedWork, "updated", connectionId, periodStart, periodEnd);
        await _notificationService.NotifyWorkUpdated(notification);

        var affectedShifts = new HashSet<(Guid ShiftId, DateOnly Date)>
        {
            (updatedWork.ShiftId, updatedWork.CurrentDate)
        };

        if (oldShiftId.HasValue && oldDate.HasValue &&
            (oldShiftId.Value != updatedWork.ShiftId || oldDate.Value != updatedWork.CurrentDate))
        {
            affectedShifts.Add((oldShiftId.Value, oldDate.Value));
        }

        await SendShiftStatsNotificationsAsync(affectedShifts, connectionId, cancellationToken);

        var currentDate = updatedWork.CurrentDate;
        var threeDayStart = currentDate.AddDays(-1);
        var threeDayEnd = currentDate.AddDays(1);

        var scheduleEntries = await _scheduleEntriesService.GetScheduleEntriesQuery(threeDayStart, threeDayEnd)
            .Where(e => e.ClientId == updatedWork.ClientId)
            .ToListAsync(cancellationToken);

        var workResource = _scheduleMapper.ToWorkResource(updatedWork);
        workResource.PeriodHours = periodHours;
        workResource.ScheduleEntries = scheduleEntries.Select(_scheduleMapper.ToWorkScheduleResource).ToList();

        return workResource;
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
