// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.WorkChanges;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<WorkChangeResource>, WorkChangeResource?>
{
    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IWorkNotificationService _notificationService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IWorkChangeResultService _resultService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PutCommandHandler(
        IWorkChangeRepository workChangeRepository,
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IWorkNotificationService notificationService,
        IScheduleCompletionService completionService,
        IWorkChangeResultService resultService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PutCommandHandler> logger)
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

    public async Task<WorkChangeResource?> Handle(PutCommand<WorkChangeResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingWorkChange = await _workChangeRepository.GetNoTracking(request.Resource.Id);
            if (existingWorkChange == null)
            {
                _logger.LogWarning("WorkChange not found: {Id}", request.Resource.Id);
                return null;
            }

            var workChange = _scheduleMapper.ToWorkChangeEntity(request.Resource);
            var updatedWorkChange = await _workChangeRepository.Put(workChange);

            if (updatedWorkChange == null)
            {
                return null;
            }

            var work = await _workRepository.Get(updatedWorkChange.WorkId);
            if (work == null)
            {
                _logger.LogWarning("Work not found for WorkChange: {WorkId}", updatedWorkChange.WorkId);
                return _scheduleMapper.ToWorkChangeResource(updatedWorkChange);
            }

            var currentDate = work.CurrentDate;
            var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(currentDate);

            await _completionService.SaveAndTrackWithReplaceClientAsync(
                work.ClientId, work.CurrentDate, periodStart, periodEnd, updatedWorkChange.ReplaceClientId);

            var threeDayStart = currentDate.AddDays(-1);
            var threeDayEnd = currentDate.AddDays(1);

            var resource = _scheduleMapper.ToWorkChangeResource(updatedWorkChange);
            resource.PeriodStart = periodStart;
            resource.PeriodEnd = periodEnd;

            var clientResult = await _resultService.GetClientResultAsync(work.ClientId, periodStart, periodEnd, threeDayStart, threeDayEnd, cancellationToken);
            resource.ClientResults.Add(clientResult);

            if (updatedWorkChange.ReplaceClientId.HasValue)
            {
                var replaceClientResult = await _resultService.GetClientResultAsync(updatedWorkChange.ReplaceClientId.Value, periodStart, periodEnd, threeDayStart, threeDayEnd, cancellationToken);
                resource.ClientResults.Add(replaceClientResult);
            }

            var connectionId = _httpContextAccessor.HttpContext?.Request
                .Headers[HttpHeaderNames.SignalRConnectionId].FirstOrDefault() ?? string.Empty;
            var notification = _scheduleMapper.ToScheduleNotificationDto(
                work.ClientId, work.CurrentDate, ScheduleEventTypes.Updated, connectionId, periodStart, periodEnd);
            await _notificationService.NotifyScheduleUpdated(notification);

            if (updatedWorkChange.ReplaceClientId.HasValue)
            {
                var replaceNotification = _scheduleMapper.ToScheduleNotificationDto(
                    updatedWorkChange.ReplaceClientId.Value, work.CurrentDate, ScheduleEventTypes.Updated, connectionId, periodStart, periodEnd);
                await _notificationService.NotifyScheduleUpdated(replaceNotification);
            }

            return resource;
        }, "UpdateWorkChange", new { request.Resource.Id });
    }
}
