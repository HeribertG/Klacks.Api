// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Undoes a Work soft-delete. Re-runs the guards a create would run (the client-based day seal, plus the
/// seal resolved via the shift's group because the deleted Work no longer binds its client to a group;
/// sporadic capacity; hard-blocking conflicts), brings back the container children and the softening
/// rows the same delete cascaded away, clears the delete stamp, then replays the create-side commit
/// sequence: commit, overtime successors, period hours, created-notifications, and the same three-day
/// WorkResource the delete answered with. A foreign delete (caller is neither Admin nor the deleting
/// user) is answered exactly like "not found" so the endpoint reveals nothing about it.
/// </summary>
/// <param name="request">Carries the id of the soft-deleted Work</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Works;

public class RestoreWorkCommandHandler : BaseHandler, IRequestHandler<RestoreWorkCommand, WorkResource?>
{
    public const string MissingDeleteStampMessage = "The work entry carries no delete time and cannot be restored.";

    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IWorkNotificationFacade _notificationFacade;
    private readonly IContainerWorkCascadeService _cascadeService;
    private readonly IWorkSofteningRepository _softeningRepository;
    private readonly ISelectedGroupContextResolver _groupContextResolver;
    private readonly IDayLockService _dayLockService;
    private readonly IWorkWriteGuard _writeGuard;
    private readonly IWorkRestoreAuthorizer _authorizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOvertimeCascadeService _overtimeCascadeService;

    public RestoreWorkCommandHandler(
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        IScheduleCompletionService completionService,
        IWorkNotificationFacade notificationFacade,
        IContainerWorkCascadeService cascadeService,
        IWorkSofteningRepository softeningRepository,
        ISelectedGroupContextResolver groupContextResolver,
        IDayLockService dayLockService,
        IWorkWriteGuard writeGuard,
        IWorkRestoreAuthorizer authorizer,
        IUnitOfWork unitOfWork,
        IOvertimeCascadeService overtimeCascadeService,
        ILogger<RestoreWorkCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _periodHoursService = periodHoursService;
        _scheduleEntriesService = scheduleEntriesService;
        _completionService = completionService;
        _notificationFacade = notificationFacade;
        _cascadeService = cascadeService;
        _softeningRepository = softeningRepository;
        _groupContextResolver = groupContextResolver;
        _dayLockService = dayLockService;
        _writeGuard = writeGuard;
        _authorizer = authorizer;
        _unitOfWork = unitOfWork;
        _overtimeCascadeService = overtimeCascadeService;
    }

    public async Task<WorkResource?> Handle(RestoreWorkCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var work = await _workRepository.GetDeletedAsync(request.Id, cancellationToken);
            if (work == null || _authorizer.Resolve(work) == WorkRestoreAccess.Hidden)
            {
                throw new KeyNotFoundException($"Deleted work with ID {request.Id} not found.");
            }

            if (!work.DeletedTime.HasValue)
            {
                throw new InvalidRequestException(MissingDeleteStampMessage);
            }

            var deletedTime = work.DeletedTime.Value;
            var deletedBy = work.CurrentUserDeleted;

            await _dayLockService.EnsureNotLockedAsync(
                work.CurrentDate,
                work.ClientId,
                work.AnalyseToken,
                cancellationToken);
            await _dayLockService.EnsureNotLockedForShiftAsync(
                work.CurrentDate,
                work.ShiftId,
                work.AnalyseToken,
                cancellationToken);

            await _writeGuard.EnsureNoSporadicConflictAsync(work, cancellationToken);
            await _writeGuard.EnsureNoHardBlockingConflictAsync(work, cancellationToken);

            var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);

            await _cascadeService.RestoreChildrenAsync(work.Id, deletedTime, deletedBy);
            await _softeningRepository.RestoreForClientDayAsync(
                work.ClientId, work.CurrentDate, work.AnalyseToken, deletedTime, deletedBy, cancellationToken);

            await _workRepository.RestoreAsync(work);

            await _unitOfWork.CompleteAsync();
            await _overtimeCascadeService.ReprocessSuccessorsAsync(work);

            var periodHours = await _completionService.SaveAndTrackAsync(
                work.ClientId, work.CurrentDate, periodStart, periodEnd, work.AnalyseToken);

            var connectionId = _notificationFacade.GetConnectionId();
            await _notificationFacade.NotifyWorkCreatedAsync(work, connectionId, periodStart, periodEnd);
            await _notificationFacade.NotifyPeriodHoursUpdatedAsync(work.ClientId, periodStart, periodEnd, periodHours, connectionId, work.AnalyseToken);
            await _notificationFacade.NotifyShiftStatsAsync(work.ShiftId, work.CurrentDate, connectionId, work.AnalyseToken, cancellationToken);

            var currentDate = work.CurrentDate;
            var threeDayStart = currentDate.AddDays(-1);
            var threeDayEnd = currentDate.AddDays(1);

            var visibleGroupIds = await _groupContextResolver.ResolveVisibleGroupIdsAsync();
            var scheduleEntries = await _scheduleEntriesService
                .GetScheduleEntriesQuery(threeDayStart, threeDayEnd, visibleGroupIds, work.AnalyseToken)
                .Where(e => e.ClientId == work.ClientId)
                .ToListAsync(cancellationToken);

            var workResource = _scheduleMapper.ToWorkResource(work);
            workResource.PeriodHours = periodHours;
            workResource.ScheduleEntries = scheduleEntries.Select(_scheduleMapper.ToWorkScheduleResource).ToList();

            return workResource;
        },
        "restoring work",
        new { WorkId = request.Id });
    }
}
