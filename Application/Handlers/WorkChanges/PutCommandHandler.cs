// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands;
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
    private readonly IScheduleCompletionService _completionService;
    private readonly IWorkChangeResultService _resultService;
    private readonly IWorkNotificationFacade _notificationFacade;

    public PutCommandHandler(
        IWorkChangeRepository workChangeRepository,
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IScheduleCompletionService completionService,
        IWorkChangeResultService resultService,
        IWorkNotificationFacade notificationFacade,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _workChangeRepository = workChangeRepository;
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _periodHoursService = periodHoursService;
        _completionService = completionService;
        _resultService = resultService;
        _notificationFacade = notificationFacade;
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

            var previousReplaceClientId = existingWorkChange.ReplaceClientId;

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

            var replaceClientChanged = previousReplaceClientId != updatedWorkChange.ReplaceClientId;

            await _completionService.SaveAndTrackWithReplaceClientAsync(
                work.ClientId, work.CurrentDate, periodStart, periodEnd,
                updatedWorkChange.ReplaceClientId,
                work.AnalyseToken,
                replaceClientChanged ? previousReplaceClientId : null);

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

            if (replaceClientChanged && previousReplaceClientId.HasValue)
            {
                var previousReplaceClientResult = await _resultService.GetClientResultAsync(previousReplaceClientId.Value, periodStart, periodEnd, threeDayStart, threeDayEnd, cancellationToken);
                resource.ClientResults.Add(previousReplaceClientResult);
            }

            var connectionId = _notificationFacade.GetConnectionId();
            await _notificationFacade.NotifyScheduleUpdatedAsync(work.ClientId, work.CurrentDate, connectionId, periodStart, periodEnd, work.AnalyseToken);

            if (updatedWorkChange.ReplaceClientId.HasValue)
            {
                await _notificationFacade.NotifyScheduleUpdatedAsync(updatedWorkChange.ReplaceClientId.Value, work.CurrentDate, connectionId, periodStart, periodEnd, work.AnalyseToken);
            }

            if (replaceClientChanged && previousReplaceClientId.HasValue)
            {
                await _notificationFacade.NotifyScheduleUpdatedAsync(previousReplaceClientId.Value, work.CurrentDate, connectionId, periodStart, periodEnd, work.AnalyseToken);
            }

            return resource;
        }, "UpdateWorkChange", new { request.Resource.Id });
    }
}
