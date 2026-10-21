// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Runs the three Orienteering phases of the container autofill in order: lock-in + greedy selection, ant colony
/// reordering, TimeRange validation + post-insertion. Returns null when the greedy phase selects nothing, so the
/// caller can answer with the empty result instead of a route over the bases only.
/// </summary>
/// <param name="greedySelector">Resolves the lock-in shifts and selects the initial stops</param>
/// <param name="routeSequencer">Reorders the selected stops with the ant colony optimizer</param>
/// <param name="routeRefiner">Drops TimeRange violators and inserts left-over candidates</param>
/// <param name="logger">Logs the outcome of every phase</param>

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

public sealed class ContainerAutofillRoutePlanner
{
    private readonly ContainerAutofillGreedySelector _greedySelector;
    private readonly ContainerAutofillRouteSequencer _routeSequencer;
    private readonly ContainerAutofillRouteRefiner _routeRefiner;
    private readonly ILogger<ContainerAutofillRoutePlanner> _logger;

    public ContainerAutofillRoutePlanner(
        ContainerAutofillGreedySelector greedySelector,
        ContainerAutofillRouteSequencer routeSequencer,
        ContainerAutofillRouteRefiner routeRefiner,
        ILogger<ContainerAutofillRoutePlanner> logger)
    {
        _greedySelector = greedySelector;
        _routeSequencer = routeSequencer;
        _routeRefiner = routeRefiner;
        _logger = logger;
    }

    public List<int>? PlanRoute(
        ContainerAutofillRouteContext context,
        List<Location> candidateLocations,
        IReadOnlyCollection<Guid>? additionalAvailableWorkIds)
    {
        var candidateCount = candidateLocations.Count;
        var lockedIndices = _greedySelector.ResolveLockedIndices(additionalAvailableWorkIds, candidateLocations);
        if (lockedIndices.Count > 0)
        {
            _logger.LogInformation("Lock-in: {Count} shifts must be included in route", lockedIndices.Count);
        }

        var selectedIndices = _greedySelector.GreedySelection(context, candidateCount, lockedIndices);
        _logger.LogInformation("Greedy selection: {Count} shifts selected ({Locked} locked)", selectedIndices.Count, lockedIndices.Count);

        if (selectedIndices.Count == 0)
        {
            return null;
        }

        var optimizedRoute = _routeSequencer.OptimizeSelectedRoute(context.DistanceMatrix, selectedIndices, context.StartBaseIndex, context.EndBaseIndex);
        _logger.LogInformation("ACO optimization complete: route with {Count} stops", optimizedRoute.Count);

        var validatedRoute = _routeRefiner.ValidateRouteTimeRanges(context, optimizedRoute, lockedIndices);

        if (validatedRoute.Count < optimizedRoute.Count)
        {
            _logger.LogInformation("Post-ACO validation: removed {Count} stops with TimeRange/container-end violations",
                optimizedRoute.Count - validatedRoute.Count);
            optimizedRoute = validatedRoute;
        }

        var remainingCandidates = Enumerable.Range(0, candidateCount)
            .Where(i => !optimizedRoute.Contains(i))
            .ToList();

        optimizedRoute = _routeRefiner.PostOptimizationInsertion(context, optimizedRoute, remainingCandidates);
        _logger.LogInformation("Post-insertion complete: route with {Count} stops", optimizedRoute.Count);

        return optimizedRoute;
    }
}
