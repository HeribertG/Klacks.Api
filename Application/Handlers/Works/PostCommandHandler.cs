using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Hubs;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Works;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<WorkResource>, WorkResource?>
{
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkNotificationService _notificationService;
    private readonly IShiftStatsNotificationService _shiftStatsNotificationService;
    private readonly IShiftScheduleService _shiftScheduleService;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PostCommandHandler(
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IWorkNotificationService notificationService,
        IShiftStatsNotificationService shiftStatsNotificationService,
        IShiftScheduleService shiftScheduleService,
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _shiftStatsNotificationService = shiftStatsNotificationService;
        _shiftScheduleService = shiftScheduleService;
        _periodHoursService = periodHoursService;
        _scheduleEntriesService = scheduleEntriesService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<WorkResource?> Handle(PostCommand<WorkResource> request, CancellationToken cancellationToken)
    {
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
        var (createdWork, periodHours) = await _workRepository.AddWithPeriodHours(work, periodStart, periodEnd);
        await _unitOfWork.CompleteAsync();

        var connectionId = _httpContextAccessor.HttpContext?.Request
            .Headers["X-SignalR-ConnectionId"].FirstOrDefault() ?? string.Empty;
        var notification = _scheduleMapper.ToWorkNotificationDto(createdWork, "created", connectionId, periodStart, periodEnd);
        await _notificationService.NotifyWorkCreated(notification);

        if (periodHours != null)
        {
            var periodHoursNotification = new Presentation.DTOs.Notifications.PeriodHoursNotificationDto
            {
                ClientId = createdWork.ClientId,
                StartDate = periodStart,
                EndDate = periodEnd,
                Hours = periodHours.Hours,
                Surcharges = periodHours.Surcharges,
                GuaranteedHours = periodHours.GuaranteedHours,
                SourceConnectionId = connectionId
            };
            await _notificationService.NotifyPeriodHoursUpdated(periodHoursNotification);
        }

        await SendShiftStatsNotificationAsync(createdWork.ShiftId, createdWork.CurrentDate, connectionId, cancellationToken);

        var currentDate = createdWork.CurrentDate;
        var threeDayStart = currentDate.AddDays(-1);
        var threeDayEnd = currentDate.AddDays(1);

        var scheduleEntries = await _scheduleEntriesService.GetScheduleEntriesQuery(threeDayStart, threeDayEnd)
            .Where(e => e.ClientId == createdWork.ClientId)
            .ToListAsync(cancellationToken);

        var workResource = _scheduleMapper.ToWorkResource(createdWork);
        workResource.PeriodHours = periodHours;
        workResource.ScheduleEntries = scheduleEntries.Select(_scheduleMapper.ToWorkScheduleResource).ToList();

        return workResource;
    }

    private async Task SendShiftStatsNotificationAsync(
        Guid shiftId,
        DateOnly date,
        string connectionId,
        CancellationToken cancellationToken)
    {
        var shiftDatePairs = new List<(Guid ShiftId, DateOnly Date)>
        {
            (shiftId, date)
        };

        var shiftStats = await _shiftScheduleService.GetShiftSchedulePartialAsync(shiftDatePairs, cancellationToken);
        var shiftData = shiftStats.FirstOrDefault();

        if (shiftData != null)
        {
            var shiftNotification = _scheduleMapper.ToShiftStatsNotificationDto(shiftData, connectionId);
            await _shiftStatsNotificationService.NotifyShiftStatsUpdated(shiftNotification);
        }
    }
}
