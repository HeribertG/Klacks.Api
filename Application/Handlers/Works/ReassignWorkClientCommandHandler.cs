// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Works;

public class ReassignWorkClientCommandHandler : BaseHandler, IRequestHandler<ReassignWorkClientCommand, ReassignWorkClientResponse?>
{
    private readonly IWorkRepository _workRepository;
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

    public ReassignWorkClientCommandHandler(
        IWorkRepository workRepository,
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
        ILogger<ReassignWorkClientCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
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
    }

    public async Task<ReassignWorkClientResponse?> Handle(ReassignWorkClientCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingWork = await _workRepository.GetNoTracking(request.Id);
            if (existingWork == null)
            {
                throw new KeyNotFoundException($"Work with ID {request.Id} not found.");
            }

            await _dayLockService.EnsureNotLockedAsync(
                existingWork.CurrentDate, existingWork.ClientId, existingWork.AnalyseToken, cancellationToken);
            await _dayLockService.EnsureNotLockedAsync(
                existingWork.CurrentDate, request.TargetClientId, existingWork.AnalyseToken, cancellationToken);

            var work = await _workRepository.Get(request.Id);
            if (work == null)
            {
                throw new KeyNotFoundException($"Work with ID {request.Id} not found.");
            }
            work.ClientId = request.TargetClientId;

            await _cascadeService.MoveChildrenAsync(work.Id, work.CurrentDate, work.ClientId);

            var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);

            // K3/K4 cascade: commit the reassignment first, then reprocess successors of both the old and
            // new client position (their prior-hours sums read committed database state) BEFORE period
            // hours are recalculated — same ordering contract as PutCommandHandler.
            await _unitOfWork.CompleteAsync();
            await _overtimeCascadeService.ReprocessSuccessorsAsync(work, existingWork);

            var periodHours = await _completionService.SaveAndTrackMoveAsync(
                work.ClientId, work.CurrentDate, periodStart, periodEnd,
                existingWork.ClientId, existingWork.CurrentDate, work.AnalyseToken);

            var connectionId = _notificationFacade.GetConnectionId();
            await _notificationFacade.NotifyWorkUpdatedAsync(work, connectionId, periodStart, periodEnd);
            await _notificationFacade.NotifyPeriodHoursUpdatedAsync(work.ClientId, periodStart, periodEnd, periodHours, connectionId, work.AnalyseToken);

            var affectedShifts = new HashSet<(Guid ShiftId, DateOnly Date)> { (work.ShiftId, work.CurrentDate) };
            await _notificationFacade.NotifyShiftStatsAsync(affectedShifts, connectionId, work.AnalyseToken, cancellationToken);

            var threeDayStart = work.CurrentDate.AddDays(-1);
            var threeDayEnd = work.CurrentDate.AddDays(1);
            var visibleGroupIds = await _groupContextResolver.ResolveVisibleGroupIdsAsync();

            var allEntries = await _scheduleEntriesService
                .GetScheduleEntriesQuery(threeDayStart, threeDayEnd, visibleGroupIds, work.AnalyseToken)
                .Where(e => e.ClientId == work.ClientId || e.ClientId == existingWork.ClientId)
                .ToListAsync(cancellationToken);

            var workResource = _scheduleMapper.ToWorkResource(work);
            workResource.PeriodHours = periodHours;
            workResource.ScheduleEntries = allEntries
                .Where(e => e.ClientId == work.ClientId)
                .Select(_scheduleMapper.ToWorkScheduleResource)
                .ToList();

            return new ReassignWorkClientResponse
            {
                Work = workResource,
                SourceScheduleEntries = allEntries
                    .Where(e => e.ClientId == existingWork.ClientId)
                    .Select(_scheduleMapper.ToWorkScheduleResource)
                    .ToList(),
            };
        },
        "reassigning work to another client",
        new { WorkId = request.Id, request.TargetClientId });
    }
}
