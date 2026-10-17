// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lists the task shifts that can be added to a container template. A missing or hidden container is answered with
/// 404, and only task shifts the caller may see are offered, so the UI never offers a task the save would refuse.
/// </summary>
/// <param name="containerVisibilityGuard">Refuses missing and hidden containers alike</param>
/// <param name="shiftVisibilityGuard">Filters the offered task shifts by group visibility</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.ContainerTemplates;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ContainerTemplates;

public class GetAvailableTasksQueryHandler : IRequestHandler<GetAvailableTasksQuery, List<ShiftResource>>
{
    private readonly IContainerAvailableTasksService _availableTasksService;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IContainerVisibilityGuard _containerVisibilityGuard;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;

    public GetAvailableTasksQueryHandler(
        IContainerAvailableTasksService availableTasksService,
        ScheduleMapper scheduleMapper,
        IContainerVisibilityGuard containerVisibilityGuard,
        IShiftVisibilityGuard shiftVisibilityGuard)
    {
        _availableTasksService = availableTasksService;
        _scheduleMapper = scheduleMapper;
        _containerVisibilityGuard = containerVisibilityGuard;
        _shiftVisibilityGuard = shiftVisibilityGuard;
    }

    public async Task<List<ShiftResource>> Handle(GetAvailableTasksQuery request, CancellationToken cancellationToken)
    {
        await _containerVisibilityGuard.EnsureContainerVisibleAsync(request.ContainerId, cancellationToken);

        var availableTasks = await _availableTasksService.GetAvailableTasksAsync(
            request.ContainerId,
            request.Weekday,
            request.FromTime,
            request.UntilTime,
            request.SearchString,
            request.ExcludeContainerId,
            request.IsHoliday,
            request.IsWeekdayAndHoliday,
            cancellationToken: cancellationToken);

        var visibleIds = await _shiftVisibilityGuard.FilterVisibleAsync(
            availableTasks.Select(s => s.Id).ToList(), cancellationToken);

        var resources = availableTasks.Where(s => visibleIds.Contains(s.Id)).Select(s => _scheduleMapper.ToShiftResource(s)).ToList();
        return resources;
    }
}
