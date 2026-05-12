// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Common;
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
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IWorkNotificationFacade _notificationFacade;
    private readonly IContainerWorkCascadeService _cascadeService;
    private readonly ISelectedGroupContextResolver _groupContextResolver;

    public DeleteCommandHandler(
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        IScheduleCompletionService completionService,
        IWorkNotificationFacade notificationFacade,
        IContainerWorkCascadeService cascadeService,
        ISelectedGroupContextResolver groupContextResolver,
        ILogger<DeleteCommandHandler> logger)
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

            var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);

            await _cascadeService.DeleteChildrenAsync(request.Id);
            await _workRepository.Delete(request.Id);
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
