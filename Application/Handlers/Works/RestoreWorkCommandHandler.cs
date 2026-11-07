// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Undoes a Work soft-delete. Re-runs the guards a create would run (the client-based day seal, plus the
/// seal resolved via the shift's group because the deleted Work no longer binds its client to a group;
/// sporadic capacity; hard-blocking conflicts), brings back the container children and the softening
/// rows the same delete cascaded away, clears the delete stamp, then replays the create-side commit
/// sequence: commit, completion (overtime successors and period hours, IScheduleCompletionService.CompleteWorkWriteAsync;
/// a failure there is logged and repaired by a queued recalculation instead of failing the committed restore),
/// created-notifications, and the same three-day
/// WorkResource the delete answered with. A foreign delete (caller is neither Admin nor the deleting
/// user) is answered exactly like "not found" so the endpoint reveals nothing about it, and so is a Work
/// owned by a client outside the caller's group visibility. A Work on a shift of another world reads like a missing shift
/// (IWorkWriteGuard).
/// </summary>
/// <param name="request">Carries the id of the soft-deleted Work</param>
/// <param name="rowAccessGuard">Decides whether the calling user may see the owning client and the stored row's scenario</param>

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
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Handlers.Works;

public class RestoreWorkCommandHandler : BaseHandler, IRequestHandler<RestoreWorkCommand, WorkResource?>
{
    public const string MissingDeleteStampMessage = "The work entry carries no delete time and cannot be restored.";

    private readonly IWorkRepository _workRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;
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

    public RestoreWorkCommandHandler(
        IWorkRepository workRepository,
        IScheduleRowAccessGuard rowAccessGuard,
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
        ILogger<RestoreWorkCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _rowAccessGuard = rowAccessGuard;
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
    }

    public async Task<WorkResource?> Handle(RestoreWorkCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var work = await _workRepository.GetDeletedAsync(request.Id, cancellationToken);
            if (work == null
                || _authorizer.Resolve(work) == WorkRestoreAccess.Hidden
                || !await _rowAccessGuard.IsVisibleAsync(work.ClientId, work.AnalyseToken, cancellationToken))
            {
                throw new KeyNotFoundException($"Deleted work with ID {request.Id} not found.");
            }

            if (!work.DeletedTime.HasValue)
            {
                throw new InvalidRequestException(MissingDeleteStampMessage);
            }

            var deletedTime = work.DeletedTime.Value;
            var deletedBy = work.CurrentUserDeleted;

            await _writeGuard.EnsureShiftsInWorkWorldAsync([(work.ShiftId, work.AnalyseToken)], cancellationToken);

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
            var completion = await _completionService.CompleteWorkWriteAsync(work, null, periodStart, periodEnd);

            var connectionId = _notificationFacade.GetConnectionId();
            await _notificationFacade.NotifyWorkCreatedAsync(work, connectionId, periodStart, periodEnd);
            await _notificationFacade.NotifyShiftStatsAsync(work.ShiftId, work.CurrentDate, connectionId, work.AnalyseToken, cancellationToken);

            var visibleGroupIds = await _groupContextResolver.ResolveVisibleGroupIdsAsync();
            var scheduleEntries = await _scheduleEntriesService.GetScheduleEntriesAsync(
                ScheduleEntriesCriteria.RefreshWindowAround(
                    work.CurrentDate, [work.ClientId], visibleGroupIds, work.AnalyseToken),
                cancellationToken);

            var workResource = _scheduleMapper.ToWorkResource(work);
            workResource.PeriodHours = completion.PeriodHours;
            workResource.ScheduleEntries = scheduleEntries.Select(_scheduleMapper.ToWorkScheduleResource).ToList();

            return workResource;
        },
        "restoring work",
        new { WorkId = request.Id });
    }
}
