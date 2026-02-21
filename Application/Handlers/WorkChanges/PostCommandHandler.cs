using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.WorkChanges;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<WorkChangeResource>, WorkChangeResource?>
{
    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IWorkNotificationService _notificationService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PostCommandHandler(
        IWorkChangeRepository workChangeRepository,
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        IWorkNotificationService notificationService,
        IScheduleCompletionService completionService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _workChangeRepository = workChangeRepository;
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _periodHoursService = periodHoursService;
        _scheduleEntriesService = scheduleEntriesService;
        _notificationService = notificationService;
        _completionService = completionService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<WorkChangeResource?> Handle(PostCommand<WorkChangeResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var workChange = _scheduleMapper.ToWorkChangeEntity(request.Resource);
            await _workChangeRepository.Add(workChange);

            var work = await _workRepository.Get(workChange.WorkId);
            if (work == null)
            {
                _logger.LogWarning("Work not found for WorkChange: {WorkId}", workChange.WorkId);
                return _scheduleMapper.ToWorkChangeResource(workChange);
            }

            var currentDate = work.CurrentDate;
            var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(currentDate);

            await _completionService.SaveAndTrackWithReplaceClientAsync(
                work.ClientId, work.CurrentDate, periodStart, periodEnd, workChange.ReplaceClientId);

            var threeDayStart = currentDate.AddDays(-1);
            var threeDayEnd = currentDate.AddDays(1);

            var resource = _scheduleMapper.ToWorkChangeResource(workChange);
            resource.PeriodStart = periodStart;
            resource.PeriodEnd = periodEnd;

            var clientResult = await GetClientResultAsync(work.ClientId, periodStart, periodEnd, threeDayStart, threeDayEnd, cancellationToken);
            resource.ClientResults.Add(clientResult);

            if (workChange.ReplaceClientId.HasValue)
            {
                var replaceClientResult = await GetClientResultAsync(workChange.ReplaceClientId.Value, periodStart, periodEnd, threeDayStart, threeDayEnd, cancellationToken);
                resource.ClientResults.Add(replaceClientResult);
            }

            var connectionId = _httpContextAccessor.HttpContext?.Request
                .Headers[HttpHeaderNames.SignalRConnectionId].FirstOrDefault() ?? string.Empty;
            var notification = _scheduleMapper.ToScheduleNotificationDto(
                work.ClientId, work.CurrentDate, ScheduleEventTypes.Updated, connectionId, periodStart, periodEnd);
            await _notificationService.NotifyScheduleUpdated(notification);

            if (workChange.ReplaceClientId.HasValue)
            {
                var replaceNotification = _scheduleMapper.ToScheduleNotificationDto(
                    workChange.ReplaceClientId.Value, work.CurrentDate, ScheduleEventTypes.Updated, connectionId, periodStart, periodEnd);
                await _notificationService.NotifyScheduleUpdated(replaceNotification);
            }

            return resource;
        }, "CreateWorkChange", new { request.Resource.WorkId });
    }

    private async Task<WorkChangeClientResult> GetClientResultAsync(
        Guid clientId,
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly threeDayStart,
        DateOnly threeDayEnd,
        CancellationToken cancellationToken)
    {
        var periodHours = await _periodHoursService.CalculatePeriodHoursAsync(clientId, periodStart, periodEnd);

        var scheduleEntries = await _scheduleEntriesService.GetScheduleEntriesQuery(threeDayStart, threeDayEnd)
            .Where(e => e.ClientId == clientId)
            .ToListAsync(cancellationToken);

        return new WorkChangeClientResult
        {
            ClientId = clientId,
            PeriodHours = periodHours,
            ScheduleEntries = scheduleEntries.Select(_scheduleMapper.ToWorkScheduleResource).ToList()
        };
    }
}
