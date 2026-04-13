// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure greedy algorithm for the Orienteering Problem in container autofill.
/// Selects and inserts shifts greedily to maximize collected work time within a time budget.
/// </summary>

namespace Klacks.Api.Domain.Services.RouteOptimization;

public static class ContainerAutofillAlgorithm
{
    public const double GREEDY_ONSITE_COST_WEIGHT = 1.0;

    public static List<int> GreedySelect(
        double[,] durationMatrix,
        List<Location> locations,
        int startIdx,
        int endIdx,
        double fromSec,
        double endSec,
        List<TimeBlock>? unmovable = null,
        double tolerance = 0.8,
        IReadOnlyList<int>? lockedIndices = null)
    {
        var blocks = unmovable ?? [];
        var candidateCount = locations.Count - (endIdx != startIdx ? 2 : 1);
        var lockedSet = lockedIndices != null ? new HashSet<int>(lockedIndices) : [];

        var selected = new List<int>();
        var remaining = Enumerable.Range(0, candidateCount).Except(lockedSet).ToList();
        var currentPosition = startIdx;
        var usedTime = 0.0;

        foreach (var locked in lockedSet)
        {
            var (_, dep) = Advance(durationMatrix, locations, currentPosition, locked, fromSec, usedTime, blocks);
            selected.Add(locked);
            usedTime = dep - fromSec;
            currentPosition = locked;
        }

        while (remaining.Count > 0)
        {
            int best = -1;
            double bestCost = double.MaxValue;

            foreach (var c in remaining)
            {
                var travel = durationMatrix[currentPosition, c];
                var onSite = locations[c].TotalOnSiteTime.TotalSeconds;
                var returnTravel = durationMatrix[c, endIdx];

                var arrival = fromSec + usedTime + travel;
                arrival = TimeBlockScheduler.SkipOverUnmovableBlocks(arrival, blocks);

                if (!IsTimeRangeValid(locations[c], arrival, tolerance))
                    continue;

                var departure = TimeBlockScheduler.SkipOverUnmovableBlocks(arrival + onSite, blocks);

                if (departure + returnTravel > endSec)
                    continue;

                var cost = travel + GREEDY_ONSITE_COST_WEIGHT * onSite + returnTravel;
                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = c;
                }
            }

            if (best == -1) break;

            var (_, bestDep) = Advance(durationMatrix, locations, currentPosition, best, fromSec, usedTime, blocks);
            selected.Add(best);
            usedTime = bestDep - fromSec;
            currentPosition = best;
            remaining.Remove(best);
        }

        return selected;
    }

    public static List<int> PostInsert(
        double[,] durationMatrix,
        List<Location> locations,
        List<int> route,
        List<int> remaining,
        int startIdx,
        int endIdx,
        double fromSec,
        double endSec,
        double budgetSec,
        List<TimeBlock>? unmovable = null,
        double tolerance = 0.8)
    {
        if (remaining.Count == 0) return route;

        var blocks = unmovable ?? [];
        var result = new List<int>(route);
        var candidates = new List<int>(remaining);
        var changed = true;

        while (changed && candidates.Count > 0)
        {
            changed = false;
            var currentTotal = CalculateRouteTotalTime(durationMatrix, locations, result, startIdx, endIdx);
            var arrivals = CalculateArrivalTimes(durationMatrix, locations, result, startIdx, fromSec, blocks);

            int bestCandidate = -1, bestPos = -1;
            double bestCost = double.MaxValue;

            foreach (var c in candidates)
            {
                for (int pos = 0; pos <= result.Count; pos++)
                {
                    var (valid, cost) = EvaluateInsertion(
                        durationMatrix, locations, result, c, pos,
                        startIdx, endIdx, fromSec, endSec, budgetSec,
                        tolerance, currentTotal, arrivals, blocks);

                    if (valid && cost < bestCost)
                    {
                        bestCost = cost;
                        bestCandidate = c;
                        bestPos = pos;
                    }
                }
            }

            if (bestCandidate != -1)
            {
                result.Insert(bestPos, bestCandidate);
                candidates.Remove(bestCandidate);
                changed = true;
            }
        }

        return result;
    }

    private static (double arrival, double departure) Advance(
        double[,] duration, List<Location> locations,
        int from, int to, double fromSec, double usedSec, List<TimeBlock> blocks)
    {
        var arrival = fromSec + usedSec + duration[from, to];
        arrival = TimeBlockScheduler.SkipOverUnmovableBlocks(arrival, blocks);
        var departure = TimeBlockScheduler.SkipOverUnmovableBlocks(arrival + locations[to].TotalOnSiteTime.TotalSeconds, blocks);
        return (arrival, departure);
    }

    private static (bool valid, double cost) EvaluateInsertion(
        double[,] duration, List<Location> locations, List<int> route,
        int candidate, int pos, int startIdx, int endIdx,
        double fromSec, double endSec, double budgetSec, double tolerance,
        double currentTotal, List<double> arrivals, List<TimeBlock> blocks)
    {
        var onSite = locations[candidate].TotalOnSiteTime.TotalSeconds;
        var prev = pos == 0 ? startIdx : route[pos - 1];
        var next = pos == route.Count ? endIdx : route[pos];

        var segDur = duration[prev, next];
        var before = duration[prev, candidate];
        var after = duration[candidate, next];
        var insertionCost = before + onSite + after - segDur;

        if (currentTotal + insertionCost > budgetSec)
            return (false, insertionCost);

        var prevEnd = pos == 0
            ? fromSec
            : arrivals[pos - 1] + locations[route[pos - 1]].TotalOnSiteTime.TotalSeconds;

        var arrivalAtCandidate = prevEnd + before;
        if (!IsTimeRangeValid(locations[candidate], arrivalAtCandidate, tolerance))
            return (false, insertionCost);

        for (int i = pos; i < route.Count; i++)
        {
            if (!IsTimeRangeValid(locations[route[i]], arrivals[i] + insertionCost, tolerance))
                return (false, insertionCost);
        }

        var lastIdx = pos == route.Count ? candidate : (route.Count > 0 ? route[^1] : candidate);
        var lastArrival = pos == route.Count ? arrivalAtCandidate : (arrivals.Count > 0 ? arrivals[^1] + insertionCost : arrivalAtCandidate);
        var lastOnSite = locations[lastIdx].TotalOnSiteTime.TotalSeconds;
        var toEnd = duration[lastIdx, endIdx];

        if (lastArrival + lastOnSite + toEnd > endSec)
            return (false, insertionCost);

        return (true, insertionCost);
    }

    private static double CalculateRouteTotalTime(
        double[,] duration, List<Location> locations, List<int> route, int startIdx, int endIdx)
    {
        if (route.Count == 0) return 0;
        var total = duration[startIdx, route[0]];
        for (int i = 0; i < route.Count; i++)
        {
            total += locations[route[i]].TotalOnSiteTime.TotalSeconds;
            if (i < route.Count - 1) total += duration[route[i], route[i + 1]];
        }
        total += duration[route[^1], endIdx];
        return total;
    }

    public static List<double> CalculateArrivalTimes(
        double[,] duration, List<Location> locations, List<int> route,
        int startIdx, double fromSec, List<TimeBlock>? unmovable = null)
    {
        var blocks = unmovable ?? [];
        var arrivals = new List<double>();
        var current = fromSec;
        var prev = startIdx;

        foreach (var idx in route)
        {
            current += duration[prev, idx];
            current = TimeBlockScheduler.SkipOverUnmovableBlocks(current, blocks);
            arrivals.Add(current);
            current += locations[idx].TotalOnSiteTime.TotalSeconds;
            current = TimeBlockScheduler.SkipOverUnmovableBlocks(current, blocks);
            prev = idx;
        }

        return arrivals;
    }

    public static bool IsTimeRangeValid(Location location, double arrivalSec, double tolerance)
    {
        if (location.TimeRangeStart == null || location.TimeRangeEnd == null || tolerance <= 0)
            return true;

        var start = location.TimeRangeStart.Value.ToTimeSpan().TotalSeconds;
        var end = location.TimeRangeEnd.Value.ToTimeSpan().TotalSeconds;
        var buffer = (end - start) * (1.0 - tolerance);
        return arrivalSec >= start - buffer && arrivalSec <= end + buffer;
    }
}
