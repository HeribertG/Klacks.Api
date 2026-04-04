// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Works;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<WorkResource>, WorkResource?>
{
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IWorkNotificationFacade _notificationFacade;
    private readonly IContainerWorkCascadeService _cascadeService;

    public PutCommandHandler(
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        IScheduleCompletionService completionService,
        IWorkNotificationFacade notificationFacade,
        IContainerWorkCascadeService cascadeService,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _periodHoursService = periodHoursService;
        _scheduleEntriesService = scheduleEntriesService;
        _completionService = completionService;
        _notificationFacade = notificationFacade;
        _cascadeService = cascadeService;
    }

    public async Task<WorkResource?> Handle(PutCommand<WorkResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingWork = await _workRepository.GetNoTracking(request.Resource.Id);
            var oldShiftId = existingWork?.ShiftId;
            var oldDate = existingWork?.CurrentDate;
            var oldLockLevel = existingWork?.LockLevel;

            var work = _scheduleMapper.ToWorkEntity(request.Resource);

            DateOnly periodStart;
            DateOnly periodEnd;

            if (request.Resource.PeriodStart.HasValue && request.Resource.PeriodEnd.HasValue)
            {
                periodStart = request.Resource.PeriodStart.Value;
                periodEnd = request.Resource.PeriodEnd.Value;
            }
            else
            {
                (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);
            }

            var updatedWork = await _workRepository.Put(work);
            if (updatedWork == null) return null;

            if (oldDate.HasValue && oldDate.Value != updatedWork.CurrentDate)
            {
                await _cascadeService.MoveChildrenAsync(updatedWork.Id, updatedWork.CurrentDate);
            }

            if (oldLockLevel.HasValue && oldLockLevel.Value != updatedWork.LockLevel)
            {
                await _cascadeService.UpdateLockLevelAsync(updatedWork.Id, updatedWork.LockLevel, updatedWork.SealedBy);
            }

            var periodHours = await _completionService.SaveAndTrackMoveAsync(
                updatedWork.ClientId, updatedWork.CurrentDate, periodStart, periodEnd,
                existingWork?.ClientId, existingWork?.CurrentDate);

            var connectionId = _notificationFacade.GetConnectionId();
            await _notificationFacade.NotifyWorkUpdatedAsync(updatedWork, connectionId, periodStart, periodEnd);
            await _notificationFacade.NotifyPeriodHoursUpdatedAsync(updatedWork.ClientId, periodStart, periodEnd, periodHours, connectionId);

            var affectedShifts = new HashSet<(Guid ShiftId, DateOnly Date)>
            {
                (updatedWork.ShiftId, updatedWork.CurrentDate)
            };

            if (oldShiftId.HasValue && oldDate.HasValue &&
                (oldShiftId.Value != updatedWork.ShiftId || oldDate.Value != updatedWork.CurrentDate))
            {
                affectedShifts.Add((oldShiftId.Value, oldDate.Value));
            }

            await _notificationFacade.NotifyShiftStatsAsync(affectedShifts, connectionId, cancellationToken);

            var currentDate = updatedWork.CurrentDate;
            var threeDayStart = currentDate.AddDays(-1);
            var threeDayEnd = currentDate.AddDays(1);

            var scheduleEntries = await _scheduleEntriesService.GetScheduleEntriesQuery(threeDayStart, threeDayEnd)
                .Where(e => e.ClientId == updatedWork.ClientId)
                .ToListAsync(cancellationToken);

            var workResource = _scheduleMapper.ToWorkResource(updatedWork);
            workResource.PeriodHours = periodHours;
            workResource.ScheduleEntries = scheduleEntries.Select(_scheduleMapper.ToWorkScheduleResource).ToList();

            return workResource;
        }, "UpdateWork", new { request.Resource.Id });
    }

}
