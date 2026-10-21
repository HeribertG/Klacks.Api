// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Phase 1 of the container autofill: resolves the lock-in shifts to candidate indices and selects stops greedily.
/// Locked stops come first in candidate order (TimeRange violations only warn); then the cheapest feasible candidate
/// (travel + weighted on-site time + return travel) is appended until none fits its TimeRange or the container end.
/// </summary>
/// <param name="logger">Logs lock-in shifts that are missing or violate their TimeRange</param>

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

public sealed class ContainerAutofillGreedySelector
{
    private readonly ILogger<ContainerAutofillGreedySelector> _logger;

    public ContainerAutofillGreedySelector(ILogger<ContainerAutofillGreedySelector> logger)
    {
        _logger = logger;
    }

    public List<int> ResolveLockedIndices(
        IReadOnlyCollection<Guid>? additionalAvailableWorkIds,
        List<Location> candidateLocations)
    {
        if (additionalAvailableWorkIds == null || additionalAvailableWorkIds.Count == 0)
        {
            return new List<int>();
        }

        var lockedSet = new HashSet<Guid>(additionalAvailableWorkIds);
        var result = new List<int>();

        for (int i = 0; i < candidateLocations.Count; i++)
        {
            if (lockedSet.Contains(candidateLocations[i].ShiftId))
            {
                result.Add(i);
            }
        }

        if (result.Count < lockedSet.Count)
        {
            var foundIds = result.Select(i => candidateLocations[i].ShiftId);
            var missing = lockedSet.Except(foundIds);
            _logger.LogWarning("Lock-in: {Count} requested shift IDs not found in candidate locations (no geocoding or filtered out): {Ids}",
                lockedSet.Count - result.Count, string.Join(", ", missing));
        }

        return result;
    }

    public List<int> GreedySelection(
        ContainerAutofillRouteContext context,
        int candidateCount,
        IReadOnlyList<int> lockedIndices)
    {
        var selected = new List<int>();
        var remaining = Enumerable.Range(0, candidateCount).Except(lockedIndices).ToList();
        var currentPosition = context.StartBaseIndex;
        var usedTimeSeconds = 0.0;

        foreach (var locked in lockedIndices)
        {
            var (arrival, departure) = ContainerAutofillRouteTiming.AdvanceToCandidate(context, currentPosition, locked, usedTimeSeconds);

            if (!ContainerAutofillRouteTiming.IsTimeRangeValid(context.DistanceMatrix.Locations[locked], arrival, context.TimeRangeTolerance))
            {
                _logger.LogWarning("Lock-in: shift {ShiftId} has TimeRange violation but will be included anyway",
                    context.DistanceMatrix.Locations[locked].ShiftId);
            }

            selected.Add(locked);
            usedTimeSeconds = departure - context.ContainerFromTimeSeconds;
            currentPosition = locked;
        }

        while (remaining.Count > 0)
        {
            var bestCandidate = FindCheapestFeasibleCandidate(context, remaining, currentPosition, usedTimeSeconds);
            if (bestCandidate == ContainerAutofillDefaults.NoCandidate)
            {
                break;
            }

            var (_, departureFromBest) = ContainerAutofillRouteTiming.AdvanceToCandidate(context, currentPosition, bestCandidate, usedTimeSeconds);

            selected.Add(bestCandidate);
            usedTimeSeconds = departureFromBest - context.ContainerFromTimeSeconds;
            currentPosition = bestCandidate;
            remaining.Remove(bestCandidate);
        }

        return selected;
    }

    private static int FindCheapestFeasibleCandidate(
        ContainerAutofillRouteContext context, List<int> remaining, int currentPosition, double usedTimeSeconds)
    {
        int bestCandidate = ContainerAutofillDefaults.NoCandidate;
        double bestCost = double.MaxValue;

        foreach (var candidate in remaining)
        {
            var travelToCandidate = context.DistanceMatrix.DurationMatrix[currentPosition, candidate];
            var onSiteTime = context.DistanceMatrix.Locations[candidate].TotalOnSiteTime.TotalSeconds;
            var travelToEnd = context.DistanceMatrix.DurationMatrix[candidate, context.EndBaseIndex];

            var absoluteArrival = context.ContainerFromTimeSeconds + usedTimeSeconds + travelToCandidate;
            absoluteArrival = TimeBlockScheduler.SkipOverUnmovableBlocks(absoluteArrival, context.UnmovableBlocks);

            if (!ContainerAutofillRouteTiming.IsTimeRangeValid(context.DistanceMatrix.Locations[candidate], absoluteArrival, context.TimeRangeTolerance))
            {
                continue;
            }

            var departureAfterCandidate = absoluteArrival + onSiteTime;
            departureAfterCandidate = TimeBlockScheduler.SkipOverUnmovableBlocks(departureAfterCandidate, context.UnmovableBlocks);

            if (departureAfterCandidate + travelToEnd > context.ContainerEndTimeSeconds)
            {
                continue;
            }

            var totalCost = travelToCandidate + ContainerAutofillAlgorithm.GREEDY_ONSITE_COST_WEIGHT * onSiteTime + travelToEnd;
            if (totalCost < bestCost)
            {
                bestCost = totalCost;
                bestCandidate = candidate;
            }
        }

        return bestCandidate;
    }
}
