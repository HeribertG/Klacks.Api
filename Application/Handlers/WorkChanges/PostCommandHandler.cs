// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
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
    private readonly IDayLockService _dayLockService;
    private readonly IPreCommitConflictChecker _conflictChecker;
    private readonly IOnCallConfigResolver _onCallConfigResolver;

    public PostCommandHandler(
        IWorkChangeRepository workChangeRepository,
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IWorkNotificationService notificationService,
        IScheduleCompletionService completionService,
        IWorkChangeResultService resultService,
        IHttpContextAccessor httpContextAccessor,
        IDayLockService dayLockService,
        IPreCommitConflictChecker conflictChecker,
        IOnCallConfigResolver onCallConfigResolver,
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
        _dayLockService = dayLockService;
        _conflictChecker = conflictChecker;
        _onCallConfigResolver = onCallConfigResolver;
    }

    public async Task<WorkChangeResource?> Handle(PostCommand<WorkChangeResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var workChange = _scheduleMapper.ToWorkChangeEntity(request.Resource);

            await ValidateOnCallAsync(workChange);

            var parentWork = await _workRepository.GetNoTracking(workChange.WorkId);
            if (parentWork == null)
            {
                throw new InvalidRequestException(
                    $"WorkChange references Work {workChange.WorkId}, which does not exist or is deleted. " +
                    "A change cannot be attached to a missing shift entry.");
            }

            // A WorkChange belongs to the same scenario as its parent Work. The resource carries
            // no AnalyseToken, so inherit it from the parent — otherwise a WorkChange created on a
            // scenario work would be persisted as real (token null) and break scenario isolation.
            workChange.AnalyseToken = parentWork.AnalyseToken;

            await _dayLockService.EnsureNotLockedAsync(
                parentWork.CurrentDate,
                parentWork.ClientId,
                workChange.AnalyseToken,
                cancellationToken);

            await EnsureNoReplacementCollisionAsync(workChange, parentWork, cancellationToken);

            await _workChangeRepository.Add(workChange);

            var currentDate = parentWork.CurrentDate;
            var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(currentDate);

            await _completionService.SaveAndTrackWithReplaceClientAsync(
                parentWork.ClientId, parentWork.CurrentDate, periodStart, periodEnd, workChange.ReplaceClientId, parentWork.AnalyseToken);

            var threeDayStart = currentDate.AddDays(-1);
            var threeDayEnd = currentDate.AddDays(1);

            var resource = _scheduleMapper.ToWorkChangeResource(workChange);
            resource.PeriodStart = periodStart;
            resource.PeriodEnd = periodEnd;

            var clientResult = await _resultService.GetClientResultAsync(parentWork.ClientId, periodStart, periodEnd, threeDayStart, threeDayEnd, cancellationToken, parentWork.AnalyseToken);
            resource.ClientResults.Add(clientResult);

            if (workChange.ReplaceClientId.HasValue)
            {
                var replaceClientResult = await _resultService.GetClientResultAsync(workChange.ReplaceClientId.Value, periodStart, periodEnd, threeDayStart, threeDayEnd, cancellationToken, parentWork.AnalyseToken);
                resource.ClientResults.Add(replaceClientResult);
            }

            var connectionId = _httpContextAccessor.HttpContext?.Request
                .Headers[HttpHeaderNames.SignalRConnectionId].FirstOrDefault() ?? string.Empty;
            var notification = _scheduleMapper.ToScheduleNotificationDto(
                parentWork.ClientId, parentWork.CurrentDate, ScheduleEventTypes.Updated, connectionId, periodStart, periodEnd, parentWork.AnalyseToken);
            await _notificationService.NotifyScheduleUpdated(notification);

            if (workChange.ReplaceClientId.HasValue)
            {
                var replaceNotification = _scheduleMapper.ToScheduleNotificationDto(
                    workChange.ReplaceClientId.Value, parentWork.CurrentDate, ScheduleEventTypes.Updated, connectionId, periodStart, periodEnd, parentWork.AnalyseToken);
                await _notificationService.NotifyScheduleUpdated(replaceNotification);
            }

            return resource;
        }, "CreateWorkChange", new { request.Resource.WorkId });
    }

    /// <summary>
    /// Hard structural-collision guard for replacement work changes: when a change moves a different
    /// client into the parent shift (ReplaceClientId set) on the real schedule (AnalyseToken null), it
    /// rejects the change if that client would end up with an overlapping shift on the same day. Pure
    /// corrections (no ReplaceClientId) and scenario writes are skipped — a correction retimes the same
    /// client's own work and would only self-collide with the parent, and scenarios run in a sandbox that
    /// bypasses the hard write guards, mirroring the day-lock contract.
    /// </summary>
    /// <param name="workChange">The change about to be persisted (times and optional ReplaceClientId)</param>
    /// <param name="parentWork">The Work the change is attached to; supplies date, shift and scenario token</param>
    private async Task EnsureNoReplacementCollisionAsync(
        Domain.Models.Schedules.WorkChange workChange,
        Domain.Models.Schedules.Work parentWork,
        CancellationToken cancellationToken)
    {
        if (workChange.AnalyseToken != null || !workChange.ReplaceClientId.HasValue)
        {
            return;
        }

        var plannedRow = new PlannedWorkRow(
            workChange.ReplaceClientId.Value,
            parentWork.CurrentDate,
            workChange.StartTime,
            workChange.EndTime,
            parentWork.ShiftId);

        var conflictCheck = await _conflictChecker.CheckAsync([plannedRow], null, cancellationToken);
        if (conflictCheck.HasHardBlocking)
        {
            throw new ConflictException(
                $"Replacement blocked: client {workChange.ReplaceClientId.Value} would have a structural " +
                $"schedule collision on {parentWork.CurrentDate:yyyy-MM-dd} (overlapping shift). Not committed.");
        }
    }

    /// <summary>
    /// Validates that an on-call (presence/standby) work change has the WorktimeOnCall feature enabled and a positive on-call window.
    /// </summary>
    /// <remarks>
    /// Validates <c>StartTime != EndTime</c> rather than an incoming ChangeTime, because the macro derives ChangeTime from this window and callers (including the LLM skill) may submit ChangeTime = 0.
    /// </remarks>
    private async Task ValidateOnCallAsync(Domain.Models.Schedules.WorkChange workChange)
    {
        if (workChange.Type is not (WorkChangeType.OnCallPresence or WorkChangeType.OnCallStandby))
        {
            return;
        }

        var onCallConfig = await _onCallConfigResolver.ResolveAsync();
        if (!onCallConfig.Enabled)
        {
            throw new InvalidRequestException(
                "On-call work changes require the WorktimeOnCall feature to be enabled.");
        }

        if (workChange.StartTime == workChange.EndTime)
        {
            throw new InvalidRequestException(
                "On-call work changes require a positive duration; start and end time must differ.");
        }
    }
}
