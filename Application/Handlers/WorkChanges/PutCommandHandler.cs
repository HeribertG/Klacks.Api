// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Updates a WorkChange and refreshes period hours and schedule entries of every client it touches. A change
/// whose parent Work (old or new) is owned by a hidden client, or whose stored or new replacement client is
/// outside the caller's group visibility, is answered exactly like a change that does not exist; nothing is written.
/// A change whose old or new parent Work is missing or deleted is answered the same way. The AnalyseToken follows the
/// parent Work (the request carries none), a change cannot be moved between a scenario and the main plan, and a sealed
/// old or new parent Work refuses the write as decided by IParentWorkLockGuard (same rule as expenses).
/// </summary>
/// <param name="rowAccessGuard">Decides whether the calling user may see the touched clients and the scenario of the parent Work</param>
/// <param name="parentWorkLockGuard">Refuses the write when a parent Work's lock level forbids it for the caller</param>
/// <param name="replacementRequestRecorder">Keeps the ManualReplacement row of the replacement request book in step with the change</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Helpers;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.WorkChanges;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<WorkChangeResource>, WorkChangeResource?>
{
    public const string CrossScopeMoveMessage =
        "A work change cannot be moved between a scenario and the main plan.";

    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly IWorkRepository _workRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IWorkChangeResultService _resultService;
    private readonly IWorkNotificationFacade _notificationFacade;
    private readonly IDayLockService _dayLockService;
    private readonly IReplacementRequestRecorder _replacementRequestRecorder;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IParentWorkLockGuard _parentWorkLockGuard;

    public PutCommandHandler(
        IWorkChangeRepository workChangeRepository,
        IWorkRepository workRepository,
        IScheduleRowAccessGuard rowAccessGuard,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IScheduleCompletionService completionService,
        IWorkChangeResultService resultService,
        IWorkNotificationFacade notificationFacade,
        IDayLockService dayLockService,
        IReplacementRequestRecorder replacementRequestRecorder,
        IHttpContextAccessor httpContextAccessor,
        IParentWorkLockGuard parentWorkLockGuard,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _workChangeRepository = workChangeRepository;
        _workRepository = workRepository;
        _rowAccessGuard = rowAccessGuard;
        _scheduleMapper = scheduleMapper;
        _periodHoursService = periodHoursService;
        _completionService = completionService;
        _resultService = resultService;
        _notificationFacade = notificationFacade;
        _dayLockService = dayLockService;
        _replacementRequestRecorder = replacementRequestRecorder;
        _httpContextAccessor = httpContextAccessor;
        _parentWorkLockGuard = parentWorkLockGuard;
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
            var isMoved = existingWorkChange.WorkId != workChange.WorkId;
            var parentWork = await _workRepository.GetNoTracking(workChange.WorkId);
            var oldParentWork = isMoved
                ? await _workRepository.GetNoTracking(existingWorkChange.WorkId)
                : parentWork;
            if (parentWork == null || oldParentWork == null)
            {
                _logger.LogWarning("Parent work of WorkChange not found: {Id}", request.Resource.Id);
                return null;
            }

            var clientIds = WorkChangeTouchedClients.Collect(
                parentWork.ClientId,
                oldParentWork.ClientId,
                existingWorkChange.ReplaceClientId,
                workChange.ReplaceClientId);
            if (!await _rowAccessGuard.AreAllVisibleAsync(clientIds, oldParentWork.AnalyseToken, cancellationToken)
                || !await _rowAccessGuard.AreAllVisibleAsync([], parentWork.AnalyseToken, cancellationToken))
            {
                return null;
            }

            if (oldParentWork.AnalyseToken != parentWork.AnalyseToken)
            {
                throw new InvalidRequestException(CrossScopeMoveMessage);
            }

            workChange.AnalyseToken = parentWork.AnalyseToken;

            await EnsureWritableAsync(parentWork, oldParentWork, isMoved, workChange, existingWorkChange, cancellationToken);

            var updatedWorkChange = await _workChangeRepository.Put(workChange);

            if (updatedWorkChange == null)
            {
                return null;
            }

            return await CompleteUpdateAsync(updatedWorkChange, previousReplaceClientId, cancellationToken);
        }, "UpdateWorkChange", new { request.Resource.Id });
    }

    /// <summary>
    /// After the write: keeps the replacement request book in step, saves and tracks the period hours of the Work's
    /// employee and of the current and replaced replacement, and answers the change with the refreshed client results
    /// after notifying every touched client. When the Work is gone, the change is answered as written without a refresh.
    /// </summary>
    /// <param name="updatedWorkChange">The change as written</param>
    /// <param name="previousReplaceClientId">Replacement client stored before the write</param>
    private async Task<WorkChangeResource> CompleteUpdateAsync(
        Domain.Models.Schedules.WorkChange updatedWorkChange,
        Guid? previousReplaceClientId,
        CancellationToken cancellationToken)
    {
        var work = await _workRepository.Get(updatedWorkChange.WorkId);
        if (work == null)
        {
            _logger.LogWarning("Work not found for WorkChange: {WorkId}", updatedWorkChange.WorkId);
            return _scheduleMapper.ToWorkChangeResource(updatedWorkChange);
        }

        await _replacementRequestRecorder.SyncManualReplacementAsync(work, updatedWorkChange, cancellationToken);

        var currentDate = work.CurrentDate;
        var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(currentDate);

        var replaceClientChanged = previousReplaceClientId != updatedWorkChange.ReplaceClientId;

        await _completionService.SaveAndTrackWithReplaceClientAsync(
            work.ClientId, work.CurrentDate, periodStart, periodEnd,
            updatedWorkChange.ReplaceClientId,
            work.AnalyseToken,
            replaceClientChanged ? previousReplaceClientId : null);

        var resource = _scheduleMapper.ToWorkChangeResource(updatedWorkChange);
        resource.PeriodStart = periodStart;
        resource.PeriodEnd = periodEnd;

        await AddClientResultsAsync(
            resource, work, updatedWorkChange, previousReplaceClientId, replaceClientChanged,
            periodStart, periodEnd, currentDate, cancellationToken);

        await NotifyTouchedClientsAsync(
            work, updatedWorkChange, previousReplaceClientId, replaceClientChanged, periodStart, periodEnd);

        return resource;
    }

    /// <summary>
    /// Refuses the write when the lock level of the new parent Work (and of the old one on a move) forbids it for the
    /// caller, or when the day of the new parent Work (and of the old one on a move) or the day the new or stored change
    /// is shown on is sealed.
    /// </summary>
    /// <param name="parentWork">Parent Work the change belongs to after the write</param>
    /// <param name="oldParentWork">Parent Work the change belonged to before the write (the same Work unless moved)</param>
    /// <param name="isMoved">True when the change is moved to another parent Work</param>
    /// <param name="workChange">The change as it will be written</param>
    /// <param name="existingWorkChange">The change as it is stored</param>
    private async Task EnsureWritableAsync(
        Domain.Models.Schedules.Work parentWork,
        Domain.Models.Schedules.Work oldParentWork,
        bool isMoved,
        Domain.Models.Schedules.WorkChange workChange,
        Domain.Models.Schedules.WorkChange existingWorkChange,
        CancellationToken cancellationToken)
    {
        _parentWorkLockGuard.EnsureChildWritableForCaller(parentWork, _httpContextAccessor);
        if (isMoved)
        {
            _parentWorkLockGuard.EnsureChildWritableForCaller(oldParentWork, _httpContextAccessor);
        }

        await _dayLockService.EnsureNotLockedAsync(
            parentWork.CurrentDate,
            parentWork.ClientId,
            parentWork.AnalyseToken,
            cancellationToken);

        if (isMoved)
        {
            await _dayLockService.EnsureNotLockedAsync(
                oldParentWork.CurrentDate,
                oldParentWork.ClientId,
                oldParentWork.AnalyseToken,
                cancellationToken);
        }

        await _dayLockService.EnsureWorkChangeNotLockedAsync(parentWork, workChange, cancellationToken);
        await _dayLockService.EnsureWorkChangeNotLockedAsync(oldParentWork, existingWorkChange, cancellationToken);
    }

    /// <summary>
    /// Adds the refreshed period hours and schedule entries of the Work's employee, of the current replacement and of a
    /// replaced previous replacement to the response, in that order.
    /// </summary>
    /// <param name="previousReplaceClientId">Replacement client stored before the write</param>
    /// <param name="replaceClientChanged">True when the write changed the replacement client</param>
    private async Task AddClientResultsAsync(
        WorkChangeResource resource,
        Domain.Models.Schedules.Work work,
        Domain.Models.Schedules.WorkChange updatedWorkChange,
        Guid? previousReplaceClientId,
        bool replaceClientChanged,
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly currentDate,
        CancellationToken cancellationToken)
    {
        var clientResult = await _resultService.GetClientResultAsync(work.ClientId, periodStart, periodEnd, currentDate, cancellationToken, work.AnalyseToken);
        resource.ClientResults.Add(clientResult);

        if (updatedWorkChange.ReplaceClientId.HasValue)
        {
            var replaceClientResult = await _resultService.GetClientResultAsync(updatedWorkChange.ReplaceClientId.Value, periodStart, periodEnd, currentDate, cancellationToken, work.AnalyseToken);
            resource.ClientResults.Add(replaceClientResult);
        }

        if (replaceClientChanged && previousReplaceClientId.HasValue)
        {
            var previousReplaceClientResult = await _resultService.GetClientResultAsync(previousReplaceClientId.Value, periodStart, periodEnd, currentDate, cancellationToken, work.AnalyseToken);
            resource.ClientResults.Add(previousReplaceClientResult);
        }
    }

    /// <summary>
    /// Broadcasts the schedule update to the Work's employee, the current replacement and a replaced previous
    /// replacement, in that order, all with the calling connection's id.
    /// </summary>
    /// <param name="previousReplaceClientId">Replacement client stored before the write</param>
    /// <param name="replaceClientChanged">True when the write changed the replacement client</param>
    private async Task NotifyTouchedClientsAsync(
        Domain.Models.Schedules.Work work,
        Domain.Models.Schedules.WorkChange updatedWorkChange,
        Guid? previousReplaceClientId,
        bool replaceClientChanged,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
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
    }
}
