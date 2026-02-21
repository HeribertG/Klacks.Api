using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.WorkChanges;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<WorkChangeResource>, WorkChangeResource?>
{
    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IWorkNotificationService _notificationService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IWorkChangeResultService _resultService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PostCommandHandler(
        IWorkChangeRepository workChangeRepository,
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IWorkNotificationService notificationService,
        IScheduleCompletionService completionService,
        IWorkChangeResultService resultService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _workChangeRepository = workChangeRepository;
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _periodHoursService = periodHoursService;
        _notificationService = notificationService;
        _completionService = completionService;
        _resultService = resultService;
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

            var clientResult = await _resultService.GetClientResultAsync(work.ClientId, periodStart, periodEnd, threeDayStart, threeDayEnd, cancellationToken);
            resource.ClientResults.Add(clientResult);

            if (workChange.ReplaceClientId.HasValue)
            {
                var replaceClientResult = await _resultService.GetClientResultAsync(workChange.ReplaceClientId.Value, periodStart, periodEnd, threeDayStart, threeDayEnd, cancellationToken);
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
}
