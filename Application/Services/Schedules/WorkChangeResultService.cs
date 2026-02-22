// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Services.Schedules;

public class WorkChangeResultService : IWorkChangeResultService
{
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly ScheduleMapper _scheduleMapper;

    public WorkChangeResultService(
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        ScheduleMapper scheduleMapper)
    {
        _periodHoursService = periodHoursService;
        _scheduleEntriesService = scheduleEntriesService;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<WorkChangeClientResult> GetClientResultAsync(
        Guid clientId,
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly threeDayStart,
        DateOnly threeDayEnd,
        CancellationToken cancellationToken)
    {
        var periodHours = await _periodHoursService.CalculatePeriodHoursAsync(clientId, periodStart, periodEnd);

        var scheduleEntries = await _scheduleEntriesService.GetScheduleEntriesQuery(threeDayStart, threeDayEnd)
            .Where(e => e.ClientId == clientId)
            .ToListAsync(cancellationToken);

        return new WorkChangeClientResult
        {
            ClientId = clientId,
            PeriodHours = periodHours,
            ScheduleEntries = scheduleEntries.Select(_scheduleMapper.ToWorkScheduleResource).ToList()
        };
    }
}
