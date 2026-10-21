// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pure timing arithmetic shared by the container autofill phases: TimeRange check, arrival times along a route,
/// total route time and the step to the next stop.
/// </summary>

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

public static class ContainerAutofillRouteTiming
{
    public static bool IsTimeRangeValid(Location location, double arrivalTimeSeconds, double timeRangeTolerance)
    {
        if (location.TimeRangeStart == null || location.TimeRangeEnd == null || timeRangeTolerance <= 0)
        {
            return true;
        }

        var windowStart = location.TimeRangeStart.Value.ToTimeSpan().TotalSeconds;
        var windowEnd = location.TimeRangeEnd.Value.ToTimeSpan().TotalSeconds;
        var windowDuration = windowEnd - windowStart;
        var relaxation = 1.0 - timeRangeTolerance;
        var buffer = windowDuration * relaxation;

        return arrivalTimeSeconds >= windowStart - buffer && arrivalTimeSeconds <= windowEnd + buffer;
    }

    public static (double arrival, double departure) AdvanceToCandidate(
        ContainerAutofillRouteContext context, int fromIndex, int toIndex, double usedTimeSeconds)
    {
        var travel = context.DistanceMatrix.DurationMatrix[fromIndex, toIndex];
        var onSite = context.DistanceMatrix.Locations[toIndex].TotalOnSiteTime.TotalSeconds;

        var arrival = context.ContainerFromTimeSeconds + usedTimeSeconds + travel;
        arrival = TimeBlockScheduler.SkipOverUnmovableBlocks(arrival, context.UnmovableBlocks);
        var departure = TimeBlockScheduler.SkipOverUnmovableBlocks(arrival + onSite, context.UnmovableBlocks);

        return (arrival, departure);
    }

    public static List<double> CalculateAbsoluteArrivalTimes(
        DistanceMatrix distanceMatrix, List<int> route, int startBaseIndex, double containerFromTimeSeconds,
        List<TimeBlock>? unmovableBlocks = null)
    {
        var blocks = unmovableBlocks ?? new List<TimeBlock>();
        var arrivalTimes = new List<double>();
        var currentTime = containerFromTimeSeconds;
        var prevIndex = startBaseIndex;

        foreach (var stopIndex in route)
        {
            currentTime += distanceMatrix.DurationMatrix[prevIndex, stopIndex];
            currentTime = TimeBlockScheduler.SkipOverUnmovableBlocks(currentTime, blocks);
            arrivalTimes.Add(currentTime);
            currentTime += distanceMatrix.Locations[stopIndex].TotalOnSiteTime.TotalSeconds;
            currentTime = TimeBlockScheduler.SkipOverUnmovableBlocks(currentTime, blocks);
            prevIndex = stopIndex;
        }

        return arrivalTimes;
    }

    public static double CalculateRouteTotalTime(
        DistanceMatrix distanceMatrix,
        List<int> route,
        int startBaseIndex,
        int endBaseIndex)
    {
        if (route.Count == 0)
        {
            return 0;
        }

        double totalSeconds = 0;

        totalSeconds += distanceMatrix.DurationMatrix[startBaseIndex, route[0]];

        for (int i = 0; i < route.Count; i++)
        {
            totalSeconds += distanceMatrix.Locations[route[i]].TotalOnSiteTime.TotalSeconds;

            if (i < route.Count - 1)
            {
                totalSeconds += distanceMatrix.DurationMatrix[route[i], route[i + 1]];
            }
        }

        totalSeconds += distanceMatrix.DurationMatrix[route.Last(), endBaseIndex];

        return totalSeconds;
    }
}
