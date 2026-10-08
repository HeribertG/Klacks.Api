// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Deletes a WorkChange and refreshes period hours and schedule entries of the clients it touched. A change
/// whose parent Work or replacement client is outside the caller's group visibility is answered exactly like
/// a change that does not exist; nothing is deleted. Scenario changes are reachable too. A sealed parent Work refuses
/// the delete as decided by IParentWorkLockGuard (same rule as expenses).
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may write for every client the change touches</param>
/// <param name="parentWorkLockGuard">Refuses the delete when the parent Work's lock level forbids it for the caller</param>
/// <param name="replacementRequestRecorder">Soft-deletes the ManualReplacement row of a deleted replacement (it did not happen)</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Helpers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.WorkChanges;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<WorkChangeResource>, WorkChangeResource?>
{
    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly IWorkRepository _workRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IWorkNotificationService _notificationService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IWorkChangeResultService _resultService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDayLockService _dayLockService;
    private readonly IReplacementRequestRecorder _replacementRequestRecorder;
    private readonly IParentWorkLockGuard _parentWorkLockGuard;

    public DeleteCommandHandler(
        IWorkChangeRepository workChangeRepository,
        IWorkRepository workRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IWorkNotificationService notificationService,
        IScheduleCompletionService completionService,
        IWorkChangeResultService resultService,
        IHttpContextAccessor httpContextAccessor,
        IDayLockService dayLockService,
        IReplacementRequestRecorder replacementRequestRecorder,
        IParentWorkLockGuard parentWorkLockGuard,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _workChangeRepository = workChangeRepository;
        _workRepository = workRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
        _periodHoursService = periodHoursService;
        _notificationService = notificationService;
        _completionService = completionService;
        _resultService = resultService;
        _httpContextAccessor = httpContextAccessor;
        _dayLockService = dayLockService;
        _replacementRequestRecorder = replacementRequestRecorder;
        _parentWorkLockGuard = parentWorkLockGuard;
    }

    public async Task<WorkChangeResource?> Handle(DeleteCommand<WorkChangeResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingWorkChange = await _workChangeRepository.GetWithWorkInAnyScope(request.Id);
            if (existingWorkChange == null)
            {
                _logger.LogWarning("WorkChange not found: {Id}", request.Id);
                return null;
            }

            var workId = existingWorkChange.WorkId;
            var replaceClientId = existingWorkChange.ReplaceClientId;
            var workChangeResource = _scheduleMapper.ToWorkChangeResource(existingWorkChange);

            var parentWork = await _workRepository.GetNoTracking(workId);
            var clientIds = new[] { parentWork?.ClientId, replaceClientId }
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();
            if (!await _clientVisibilityGuard.AreAllVisibleAsync(clientIds, cancellationToken))
            {
                return null;
            }

            if (parentWork != null)
            {
                _parentWorkLockGuard.EnsureChildWritableForCaller(parentWork, _httpContextAccessor);

                await _dayLockService.EnsureNotLockedAsync(
                    parentWork.CurrentDate,
                    parentWork.ClientId,
                    parentWork.AnalyseToken,
                    cancellationToken);
            }

            await _workChangeRepository.Delete(request.Id);
            await _replacementRequestRecorder.DiscardManualReplacementAsync(request.Id, cancellationToken);

            var work = await _workRepository.Get(workId);
            if (work == null)
            {
                _logger.LogWarning("Work not found for WorkChange: {WorkId}", workId);
                return workChangeResource;
            }

            var currentDate = work.CurrentDate;
            var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(currentDate);

            await _completionService.SaveAndTrackWithReplaceClientAsync(
                work.ClientId, work.CurrentDate, periodStart, periodEnd, replaceClientId, work.AnalyseToken);

            var threeDayStart = currentDate.AddDays(-1);
            var threeDayEnd = currentDate.AddDays(1);

            workChangeResource.PeriodStart = periodStart;
            workChangeResource.PeriodEnd = periodEnd;

            var clientResult = await _resultService.GetClientResultAsync(work.ClientId, periodStart, periodEnd, threeDayStart, threeDayEnd, cancellationToken, work.AnalyseToken);
            workChangeResource.ClientResults.Add(clientResult);

            if (replaceClientId.HasValue)
            {
                var replaceClientResult = await _resultService.GetClientResultAsync(replaceClientId.Value, periodStart, periodEnd, threeDayStart, threeDayEnd, cancellationToken, work.AnalyseToken);
                workChangeResource.ClientResults.Add(replaceClientResult);
            }

            var connectionId = _httpContextAccessor.HttpContext?.Request
                .Headers[HttpHeaderNames.SignalRConnectionId].FirstOrDefault() ?? string.Empty;
            var notification = _scheduleMapper.ToScheduleNotificationDto(
                work.ClientId, work.CurrentDate, ScheduleEventTypes.Updated, connectionId, periodStart, periodEnd, work.AnalyseToken);
            await _notificationService.NotifyScheduleUpdated(notification);

            if (replaceClientId.HasValue)
            {
                var replaceNotification = _scheduleMapper.ToScheduleNotificationDto(
                    replaceClientId.Value, work.CurrentDate, ScheduleEventTypes.Updated, connectionId, periodStart, periodEnd, work.AnalyseToken);
                await _notificationService.NotifyScheduleUpdated(replaceNotification);
            }

            return workChangeResource;
        }, "DeleteWorkChange", new { request.Id });
    }
}
