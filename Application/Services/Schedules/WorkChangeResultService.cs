// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public class WorkChangeResultService : IWorkChangeResultService
{
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly ISelectedGroupContextResolver _groupContextResolver;

    public WorkChangeResultService(
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        ScheduleMapper scheduleMapper,
        ISelectedGroupContextResolver groupContextResolver)
    {
        _periodHoursService = periodHoursService;
        _scheduleEntriesService = scheduleEntriesService;
        _scheduleMapper = scheduleMapper;
        _groupContextResolver = groupContextResolver;
    }

    public async Task<WorkChangeClientResult> GetClientResultAsync(
        Guid clientId,
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly workDay,
        CancellationToken cancellationToken,
        Guid? analyseToken = null)
    {
        var periodHours = await _periodHoursService.CalculatePeriodHoursAsync(clientId, periodStart, periodEnd, analyseToken);

        var visibleGroupIds = await _groupContextResolver.ResolveVisibleGroupIdsAsync();
        var scheduleEntries = await _scheduleEntriesService.GetScheduleEntriesAsync(
            ScheduleEntriesCriteria.RefreshWindowAround(workDay, [clientId], visibleGroupIds, analyseToken),
            cancellationToken);

        return new WorkChangeClientResult
        {
            ClientId = clientId,
            PeriodHours = periodHours,
            ScheduleEntries = scheduleEntries.Select(_scheduleMapper.ToWorkScheduleResource).ToList()
        };
    }
}
