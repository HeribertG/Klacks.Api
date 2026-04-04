// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands.Works;
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
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IWorkNotificationFacade _notificationFacade;
    private readonly IContainerWorkCascadeService _cascadeService;

    public DeleteCommandHandler(
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IScheduleEntriesService scheduleEntriesService,
        IScheduleCompletionService completionService,
        IWorkNotificationFacade notificationFacade,
        IContainerWorkCascadeService cascadeService,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _scheduleEntriesService = scheduleEntriesService;
        _completionService = completionService;
        _notificationFacade = notificationFacade;
        _cascadeService = cascadeService;
    }

    public async Task<WorkResource?> Handle(DeleteWorkCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var work = await _workRepository.Get(request.Id);
            if (work == null)
            {
                throw new KeyNotFoundException($"Work with ID {request.Id} not found.");
            }

            var shiftId = work.ShiftId;
            var workDate = work.CurrentDate;

            await _cascadeService.DeleteChildrenAsync(request.Id);
            await _workRepository.Delete(request.Id);
            var periodHours = await _completionService.SaveAndTrackAsync(
                work.ClientId, work.CurrentDate, request.PeriodStart, request.PeriodEnd);

            var connectionId = _notificationFacade.GetConnectionId();
            await _notificationFacade.NotifyWorkDeletedAsync(work, connectionId, request.PeriodStart, request.PeriodEnd);
            await _notificationFacade.NotifyPeriodHoursUpdatedAsync(work.ClientId, request.PeriodStart, request.PeriodEnd, periodHours, connectionId);
            await _notificationFacade.NotifyShiftStatsAsync(shiftId, workDate, connectionId, cancellationToken);

            var currentDate = work.CurrentDate;
            var threeDayStart = currentDate.AddDays(-1);
            var threeDayEnd = currentDate.AddDays(1);

            var scheduleEntries = await _scheduleEntriesService.GetScheduleEntriesQuery(threeDayStart, threeDayEnd)
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
