// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Phase 3 of the container autofill: after the ant colony reordering, drops unlocked stops that miss their TimeRange
/// (or, as last stop, the container end), then inserts left-over candidates at their cheapest feasible position.
/// </summary>
/// <param name="logger">Logs every post-insertion</param>

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

public sealed class ContainerAutofillRouteRefiner
{
    private const int NoViolation = -1;
    private const int NoPosition = -1;

    private readonly ILogger<ContainerAutofillRouteRefiner> _logger;

    public ContainerAutofillRouteRefiner(ILogger<ContainerAutofillRouteRefiner> logger)
    {
        _logger = logger;
    }

    public List<int> ValidateRouteTimeRanges(
        ContainerAutofillRouteContext context,
        List<int> route,
        IReadOnlyList<int>? lockedIndices = null)
    {
        var lockedSet = lockedIndices != null ? new HashSet<int>(lockedIndices) : new HashSet<int>();
        var validRoute = new List<int>(route);

        while (true)
        {
            var arrivalTimes = ContainerAutofillRouteTiming.CalculateAbsoluteArrivalTimes(
                context.DistanceMatrix, validRoute, context.StartBaseIndex, context.ContainerFromTimeSeconds, context.UnmovableBlocks);

            var violatingStop = FindLastRemovableViolation(context, validRoute, arrivalTimes, lockedSet);
            if (violatingStop == NoViolation)
            {
                return validRoute;
            }

            validRoute.RemoveAt(violatingStop);
        }
    }

    public List<int> PostOptimizationInsertion(
        ContainerAutofillRouteContext context,
        List<int> currentRoute,
        List<int> remainingCandidates)
    {
        if (remainingCandidates.Count == 0)
        {
            return currentRoute;
        }

        var route = new List<int>(currentRoute);
        var changed = true;

        while (changed && remainingCandidates.Count > 0)
        {
            changed = false;
            var currentTotalTime = ContainerAutofillRouteTiming.CalculateRouteTotalTime(context.DistanceMatrix, route, context.StartBaseIndex, context.EndBaseIndex);
            var currentArrivalTimes = ContainerAutofillRouteTiming.CalculateAbsoluteArrivalTimes(context.DistanceMatrix, route, context.StartBaseIndex, context.ContainerFromTimeSeconds, context.UnmovableBlocks);

            var (bestCandidate, bestPosition, _) = EvaluateInsertionCandidates(
                context, route, remainingCandidates, currentTotalTime, currentArrivalTimes);

            if (bestCandidate != ContainerAutofillDefaults.NoCandidate)
            {
                route.Insert(bestPosition, bestCandidate);
                remainingCandidates.Remove(bestCandidate);
                changed = true;
                _logger.LogInformation("Post-insertion: added shift at index {Index} to position {Position}", bestCandidate, bestPosition);
            }
        }

        return route;
    }

    private static int FindLastRemovableViolation(
        ContainerAutofillRouteContext context, List<int> route, List<double> arrivalTimes, HashSet<int> lockedSet)
    {
        for (int i = route.Count - 1; i >= 0; i--)
        {
            var location = context.DistanceMatrix.Locations[route[i]];
            var arrival = arrivalTimes[i];
            var onSite = location.TotalOnSiteTime.TotalSeconds;
            var travelToEnd = context.DistanceMatrix.DurationMatrix[route[i], context.EndBaseIndex];
            var isLastStop = i == route.Count - 1;

            var timeRangeViolation = !ContainerAutofillRouteTiming.IsTimeRangeValid(location, arrival, context.TimeRangeTolerance);
            var containerEndViolation = isLastStop && arrival + onSite + travelToEnd > context.ContainerEndTimeSeconds;

            if ((timeRangeViolation || containerEndViolation) && !lockedSet.Contains(route[i]))
            {
                return i;
            }
        }

        return NoViolation;
    }

    private static (int candidate, int position, double cost) EvaluateInsertionCandidates(
        ContainerAutofillRouteContext context,
        List<int> route,
        List<int> remainingCandidates,
        double currentTotalTime,
        List<double> currentArrivalTimes)
    {
        int bestCandidate = ContainerAutofillDefaults.NoCandidate;
        int bestPosition = NoPosition;
        double bestInsertionCost = double.MaxValue;

        foreach (var candidate in remainingCandidates)
        {
            for (int pos = 0; pos <= route.Count; pos++)
            {
                var (isValid, insertionCost) = EvaluateInsertionPosition(
                    context, route, candidate, pos, currentTotalTime, currentArrivalTimes);

                if (isValid && insertionCost < bestInsertionCost)
                {
                    bestInsertionCost = insertionCost;
                    bestCandidate = candidate;
                    bestPosition = pos;
                }
            }
        }

        return (bestCandidate, bestPosition, bestInsertionCost);
    }

    private static (bool isValid, double insertionCost) EvaluateInsertionPosition(
        ContainerAutofillRouteContext context,
        List<int> route,
        int candidate,
        int pos,
        double currentTotalTime,
        List<double> currentArrivalTimes)
    {
        var onSiteTime = context.DistanceMatrix.Locations[candidate].TotalOnSiteTime.TotalSeconds;
        var prevIndex = pos == 0 ? context.StartBaseIndex : route[pos - 1];
        var nextIndex = pos == route.Count ? context.EndBaseIndex : route[pos];

        var currentSegmentDuration = context.DistanceMatrix.DurationMatrix[prevIndex, nextIndex];
        var newDurationBefore = context.DistanceMatrix.DurationMatrix[prevIndex, candidate];
        var newDurationAfter = context.DistanceMatrix.DurationMatrix[candidate, nextIndex];
        var insertionCost = newDurationBefore + onSiteTime + newDurationAfter - currentSegmentDuration;

        if (currentTotalTime + insertionCost > context.BudgetSeconds)
        {
            return (false, insertionCost);
        }

        var prevArrivalEnd = pos == 0
            ? context.ContainerFromTimeSeconds
            : currentArrivalTimes[pos - 1] + context.DistanceMatrix.Locations[route[pos - 1]].TotalOnSiteTime.TotalSeconds;

        var candidateArrival = prevArrivalEnd + newDurationBefore;
        if (!ContainerAutofillRouteTiming.IsTimeRangeValid(context.DistanceMatrix.Locations[candidate], candidateArrival, context.TimeRangeTolerance))
        {
            return (false, insertionCost);
        }

        var isValid = FollowingStopsStayInTimeRange(context, route, pos, insertionCost, currentArrivalTimes)
            && LastStopReturnsBeforeContainerEnd(context, route, candidate, pos, candidateArrival, insertionCost, currentArrivalTimes);

        return (isValid, insertionCost);
    }

    private static bool FollowingStopsStayInTimeRange(
        ContainerAutofillRouteContext context, List<int> route, int pos, double timeShift, List<double> currentArrivalTimes)
    {
        for (int i = pos; i < route.Count; i++)
        {
            var shiftedArrival = currentArrivalTimes[i] + timeShift;
            if (!ContainerAutofillRouteTiming.IsTimeRangeValid(context.DistanceMatrix.Locations[route[i]], shiftedArrival, context.TimeRangeTolerance))
            {
                return false;
            }
        }

        return true;
    }

    private static bool LastStopReturnsBeforeContainerEnd(
        ContainerAutofillRouteContext context,
        List<int> route,
        int candidate,
        int pos,
        double candidateArrival,
        double timeShift,
        List<double> currentArrivalTimes)
    {
        var (lastIndex, lastArrival) = pos == route.Count
            ? (candidate, candidateArrival)
            : (route[route.Count - 1], currentArrivalTimes[route.Count - 1] + timeShift);
        var lastOnSite = context.DistanceMatrix.Locations[lastIndex].TotalOnSiteTime.TotalSeconds;
        var travelToEnd = context.DistanceMatrix.DurationMatrix[lastIndex, context.EndBaseIndex];

        return !(lastArrival + lastOnSite + travelToEnd > context.ContainerEndTimeSeconds);
    }
}
