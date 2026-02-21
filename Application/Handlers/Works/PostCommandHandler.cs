using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Works;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<WorkResource>, WorkResource?>
{
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IWorkNotificationService _notificationService;
    private readonly IShiftStatsNotificationService _shiftStatsNotificationService;
    private readonly IShiftScheduleService _shiftScheduleService;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PostCommandHandler(
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IWorkNotificationService notificationService,
        IShiftStatsNotificationService shiftStatsNotificationService,
        IShiftScheduleService shiftScheduleService,
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        IScheduleCompletionService completionService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _notificationService = notificationService;
        _shiftStatsNotificationService = shiftStatsNotificationService;
        _shiftScheduleService = shiftScheduleService;
        _periodHoursService = periodHoursService;
        _scheduleEntriesService = scheduleEntriesService;
        _completionService = completionService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<WorkResource?> Handle(PostCommand<WorkResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
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

            await _workRepository.Add(work);
            var periodHours = await _completionService.SaveAndTrackAsync(
                work.ClientId, work.CurrentDate, periodStart, periodEnd);

            var connectionId = _httpContextAccessor.HttpContext?.Request
                .Headers[HttpHeaderNames.SignalRConnectionId].FirstOrDefault() ?? string.Empty;
            var notification = _scheduleMapper.ToWorkNotificationDto(work, ScheduleEventTypes.Created, connectionId, periodStart, periodEnd);
            await _notificationService.NotifyWorkCreated(notification);

            var periodHoursNotification = new Application.DTOs.Notifications.PeriodHoursNotificationDto
            {
                ClientId = work.ClientId,
                StartDate = periodStart,
                EndDate = periodEnd,
                Hours = periodHours.Hours,
                Surcharges = periodHours.Surcharges,
                GuaranteedHours = periodHours.GuaranteedHours,
                SourceConnectionId = connectionId
            };
            await _notificationService.NotifyPeriodHoursUpdated(periodHoursNotification);

            await SendShiftStatsNotificationAsync(work.ShiftId, work.CurrentDate, connectionId, cancellationToken);

            var currentDate = work.CurrentDate;
            var threeDayStart = currentDate.AddDays(-1);
            var threeDayEnd = currentDate.AddDays(1);

            var scheduleEntries = await _scheduleEntriesService.GetScheduleEntriesQuery(threeDayStart, threeDayEnd)
                .Where(e => e.ClientId == work.ClientId)
                .ToListAsync(cancellationToken);

            var workResource = _scheduleMapper.ToWorkResource(work);
            workResource.PeriodHours = periodHours;
            workResource.ScheduleEntries = scheduleEntries.Select(_scheduleMapper.ToWorkScheduleResource).ToList();

            return workResource;
        }, "CreateWork", new { request.Resource.ClientId });
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
