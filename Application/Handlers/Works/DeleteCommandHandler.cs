// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Soft-deletes a Work with its container children and notifies the schedule. A Work owned by a client
/// outside the caller's group visibility is refused exactly like a Work that does not exist; nothing is
/// deleted. On top of DeleteWorkCommandValidator (non-admins may only delete unsealed Works), IParentWorkLockGuard
/// refuses a Closed Work for everyone, admins included: deleting it would also drop its expenses and WorkChanges
/// from a closed period.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may write for the owning client</param>
/// <param name="workLockGuard">Refuses the delete when the Work's lock level forbids it for the caller</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Helpers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Works;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteWorkCommand, WorkResource?>
{
    private readonly IWorkRepository _workRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IWorkNotificationFacade _notificationFacade;
    private readonly IContainerWorkCascadeService _cascadeService;
    private readonly ISelectedGroupContextResolver _groupContextResolver;
    private readonly IDayLockService _dayLockService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOvertimeCascadeService _overtimeCascadeService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IParentWorkLockGuard _workLockGuard;

    public DeleteCommandHandler(
        IWorkRepository workRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        IScheduleCompletionService completionService,
        IWorkNotificationFacade notificationFacade,
        IContainerWorkCascadeService cascadeService,
        ISelectedGroupContextResolver groupContextResolver,
        IDayLockService dayLockService,
        IUnitOfWork unitOfWork,
        IOvertimeCascadeService overtimeCascadeService,
        IHttpContextAccessor httpContextAccessor,
        IParentWorkLockGuard workLockGuard,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
        _periodHoursService = periodHoursService;
        _scheduleEntriesService = scheduleEntriesService;
        _completionService = completionService;
        _notificationFacade = notificationFacade;
        _cascadeService = cascadeService;
        _groupContextResolver = groupContextResolver;
        _dayLockService = dayLockService;
        _unitOfWork = unitOfWork;
        _overtimeCascadeService = overtimeCascadeService;
        _httpContextAccessor = httpContextAccessor;
        _workLockGuard = workLockGuard;
    }

    public async Task<WorkResource?> Handle(DeleteWorkCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var work = await _workRepository.Get(request.Id);
            if (work == null || !await _clientVisibilityGuard.IsVisibleAsync(work.ClientId, cancellationToken))
            {
                throw new KeyNotFoundException($"Work with ID {request.Id} not found.");
            }

            _workLockGuard.EnsureWorkWritableForCaller(work, _httpContextAccessor);

            await _dayLockService.EnsureNotLockedAsync(
                work.CurrentDate,
                work.ClientId,
                work.AnalyseToken,
                cancellationToken);

            var shiftId = work.ShiftId;
            var workDate = work.CurrentDate;

            var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);

            await _cascadeService.DeleteChildrenAsync(request.Id);
            await _workRepository.Delete(request.Id);

            // K3/K4 cascade: commit the soft-delete first, then reprocess the successor Works in the
            // deleted Work's overtime basis period (its former prior-hours contribution is gone; the
            // detached entity still carries the client/date/start time that identify the period and
            // ordering position) BEFORE period hours are recalculated.
            await _unitOfWork.CompleteAsync();
            await _overtimeCascadeService.ReprocessSuccessorsAsync(work);

            var periodHours = await _completionService.SaveAndTrackAsync(
                work.ClientId, work.CurrentDate, periodStart, periodEnd, work.AnalyseToken);

            var connectionId = _notificationFacade.GetConnectionId();
            await _notificationFacade.NotifyWorkDeletedAsync(work, connectionId, periodStart, periodEnd);
            await _notificationFacade.NotifyPeriodHoursUpdatedAsync(work.ClientId, periodStart, periodEnd, periodHours, connectionId, work.AnalyseToken);
            await _notificationFacade.NotifyShiftStatsAsync(shiftId, workDate, connectionId, work.AnalyseToken, cancellationToken);

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
        "deleting work",
        new { WorkId = request.Id });
    }
}
