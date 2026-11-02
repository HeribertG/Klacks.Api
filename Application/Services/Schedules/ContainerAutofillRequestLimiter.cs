// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Narrows an autofill request from a caller to what the caller may read, before the route is calculated, so a hidden
/// shift neither shows up in the answer nor shapes it or its candidate count. The override ids keep the rule of the route
/// requests (visible real shifts and clones of a visible scenario, no shift of a key for any caller); the candidates of
/// the slot keep the rule of the available-task list and are passed on as the restriction of the autofill. A hidden and a
/// missing shift are both left out without a word, so the answer does not tell them apart.
/// </summary>
/// <param name="availableTasksService">Reads the candidates of the slot</param>
/// <param name="shiftVisibilityGuard">Filters the candidates by group visibility</param>
/// <param name="routeShiftVisibilityFilter">Filters the override ids of the request</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Services.RouteOptimization;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class ContainerAutofillRequestLimiter
{
    private readonly IContainerAvailableTasksService _availableTasksService;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;
    private readonly IRouteShiftVisibilityFilter _routeShiftVisibilityFilter;

    public ContainerAutofillRequestLimiter(
        IContainerAvailableTasksService availableTasksService,
        IShiftVisibilityGuard shiftVisibilityGuard,
        IRouteShiftVisibilityFilter routeShiftVisibilityFilter)
    {
        _availableTasksService = availableTasksService;
        _shiftVisibilityGuard = shiftVisibilityGuard;
        _routeShiftVisibilityFilter = routeShiftVisibilityFilter;
    }

    /// <param name="request">Autofill request as the caller sent it</param>
    /// <returns>The same request with the override ids and the candidates the caller may read</returns>
    public async Task<ContainerAutofillRequest> LimitToReadableAsync(ContainerAutofillRequest request)
    {
        var overrideIds = await _routeShiftVisibilityFilter.FilterAsync(
            request.AdditionalAvailableWorkIds ?? [], request.CancellationToken);

        var candidates = await _availableTasksService.GetAvailableTasksAsync(
            request.ContainerId, request.Weekday, request.FromTime, request.UntilTime,
            isHoliday: request.IsHoliday,
            isWeekdayAndHoliday: request.IsWeekdayAndHoliday,
            restrictToShiftIds: request.RestrictToShiftIds,
            cancellationToken: request.CancellationToken);

        var candidateIds = candidates.Select(task => task.Id).ToList();
        var visibleIds = await _shiftVisibilityGuard.FilterVisibleAsync(candidateIds, request.CancellationToken);

        return request with
        {
            AdditionalAvailableWorkIds = overrideIds,
            RestrictToShiftIds = candidateIds.Where(visibleIds.Contains).ToList()
        };
    }
}
