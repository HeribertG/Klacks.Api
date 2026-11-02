// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Applies the shift group visibility and the scenario token rule to the shift ids of a route request.
/// </summary>
/// <param name="shiftVisibilityGuard">Filters the ids by the shift group visibility of the caller</param>
/// <param name="shiftVisibilityReader">Tells which of the ids carry an analyse token, and which one</param>
/// <param name="scenarioTokenAccessGuard">Decides per token whether the caller may work in that world; a key token is refused for every caller</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public class RouteShiftVisibilityFilter : IRouteShiftVisibilityFilter
{
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;
    private readonly IShiftVisibilityReader _shiftVisibilityReader;
    private readonly IScenarioTokenAccessGuard _scenarioTokenAccessGuard;

    public RouteShiftVisibilityFilter(
        IShiftVisibilityGuard shiftVisibilityGuard,
        IShiftVisibilityReader shiftVisibilityReader,
        IScenarioTokenAccessGuard scenarioTokenAccessGuard)
    {
        _shiftVisibilityGuard = shiftVisibilityGuard;
        _shiftVisibilityReader = shiftVisibilityReader;
        _scenarioTokenAccessGuard = scenarioTokenAccessGuard;
    }

    public async Task<List<Guid>> FilterAsync(IReadOnlyCollection<Guid> shiftIds, CancellationToken cancellationToken = default)
    {
        var distinctIds = shiftIds.Distinct().ToList();
        var visibleIds = await _shiftVisibilityGuard.FilterVisibleAsync(distinctIds, cancellationToken);
        var worlds = await _shiftVisibilityReader.GetTokenedShiftWorldsAsync(visibleIds.ToList(), cancellationToken);

        var refusedIds = new HashSet<Guid>();
        foreach (var world in worlds.GroupBy(row => row.AnalyseToken))
        {
            if (!await _scenarioTokenAccessGuard.IsVisibleAsync(world.Key, cancellationToken))
            {
                refusedIds.UnionWith(world.Select(row => row.ShiftId));
            }
        }

        return distinctIds.Where(id => visibleIds.Contains(id) && !refusedIds.Contains(id)).ToList();
    }
}
