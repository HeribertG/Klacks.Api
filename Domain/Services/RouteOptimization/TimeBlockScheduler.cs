// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Scheduler for TimeBlocks in route optimization.
/// Calculates effective time budget, skips fixed blocks and places flexible blocks optimally.
/// </summary>

namespace Klacks.Api.Domain.Services.RouteOptimization;

public static class TimeBlockScheduler
{
    private const double SecondsPerMinute = 60.0;
    private const double SecondsPerDay = 86400.0;

    public static double SkipOverUnmovableBlocks(double timeSeconds, List<TimeBlock> unmovableBlocks)
    {
        var currentTime = timeSeconds;

        foreach (var block in unmovableBlocks.OrderBy(b => b.FixedStartTime!.Value.ToTimeSpan().TotalSeconds))
        {
            var blockStart = block.FixedStartTime!.Value.ToTimeSpan().TotalSeconds;
            var blockEnd = block.FixedEndTime!.Value.ToTimeSpan().TotalSeconds;
            if (blockEnd <= blockStart)
            {
                blockEnd += SecondsPerDay;
            }

            if (currentTime >= blockStart && currentTime < blockEnd)
            {
                currentTime = blockEnd;
            }
        }

        return currentTime;
    }

    public static double CalculateEffectiveBudget(double totalBudgetSeconds, List<TimeBlock> allBlocks)
    {
        var totalBlockDuration = allBlocks.Sum(b => b.Duration.TotalSeconds);
        return Math.Max(0, totalBudgetSeconds - totalBlockDuration);
    }

    public static List<PlacedTimeBlock> PlaceUnmovableBlocks(
        List<TimeBlock> unmovableBlocks,
        List<int> route,
        DistanceMatrix distanceMatrix,
        int startBaseIndex,
        double containerFromTimeSeconds)
    {
        var placed = new List<PlacedTimeBlock>();

        foreach (var block in unmovableBlocks)
        {
            var blockStartSeconds = block.FixedStartTime!.Value.ToTimeSpan().TotalSeconds;
            var blockEndSeconds = block.FixedEndTime!.Value.ToTimeSpan().TotalSeconds;
            if (blockEndSeconds <= blockStartSeconds)
            {
                blockEndSeconds += SecondsPerDay;
            }

            var position = FindInsertionPositionForTime(blockStartSeconds, route, distanceMatrix, startBaseIndex, containerFromTimeSeconds);

            placed.Add(new PlacedTimeBlock(block, blockStartSeconds, blockEndSeconds, position));
        }

        return placed;
    }

    public static List<PlacedTimeBlock> PlaceMovableBlocks(
        List<TimeBlock> movableBlocks,
        List<int> route,
        DistanceMatrix distanceMatrix,
        int startBaseIndex,
        int endBaseIndex,
        double containerFromTimeSeconds,
        List<PlacedTimeBlock> alreadyPlaced)
    {
        var placed = new List<PlacedTimeBlock>(alreadyPlaced);

        foreach (var block in movableBlocks)
        {
            var bestPosition = -1;
            var bestGapSize = -1.0;

            for (int pos = 0; pos <= route.Count; pos++)
            {
                var departureTime = CalculateDepartureTimeAtPosition(
                    pos, route, distanceMatrix, startBaseIndex, containerFromTimeSeconds, placed);

                var nextArrivalNeeded = pos < route.Count
                    ? CalculateNextRequiredTime(pos, route, distanceMatrix, startBaseIndex, endBaseIndex, containerFromTimeSeconds, placed)
                    : double.MaxValue;

                var gapAvailable = nextArrivalNeeded - departureTime;

                if (gapAvailable >= block.Duration.TotalSeconds && gapAvailable > bestGapSize)
                {
                    bestGapSize = gapAvailable;
                    bestPosition = pos;
                }
            }

            if (bestPosition == -1)
            {
                bestPosition = route.Count;
            }

            var startTime = CalculateDepartureTimeAtPosition(
                bestPosition, route, distanceMatrix, startBaseIndex, containerFromTimeSeconds, placed);
            var endTime = startTime + block.Duration.TotalSeconds;

            placed.Add(new PlacedTimeBlock(block, startTime, endTime, bestPosition));
        }

        return placed.Where(p => p.Block.IsMovable).ToList();
    }

    public static int FindInsertionPositionForTime(
        double timeSeconds,
        List<int> route,
        DistanceMatrix distanceMatrix,
        int startBaseIndex,
        double containerFromTimeSeconds)
    {
        var currentTime = containerFromTimeSeconds;
        var prevIndex = startBaseIndex;

        for (int i = 0; i < route.Count; i++)
        {
            currentTime += distanceMatrix.DurationMatrix[prevIndex, route[i]];
            currentTime += distanceMatrix.Locations[route[i]].TotalOnSiteTime.TotalSeconds;

            if (currentTime > timeSeconds)
            {
                return i;
            }

            prevIndex = route[i];
        }

        return route.Count;
    }

    public static List<double> CalculateArrivalTimesWithBlocks(
        DistanceMatrix distanceMatrix,
        List<int> route,
        int startBaseIndex,
        double containerFromTimeSeconds,
        List<TimeBlock> unmovableBlocks)
    {
        var arrivalTimes = new List<double>();
        var currentTime = containerFromTimeSeconds;
        var prevIndex = startBaseIndex;

        foreach (var stopIndex in route)
        {
            currentTime += distanceMatrix.DurationMatrix[prevIndex, stopIndex];
            currentTime = SkipOverUnmovableBlocks(currentTime, unmovableBlocks);
            arrivalTimes.Add(currentTime);
            currentTime += distanceMatrix.Locations[stopIndex].TotalOnSiteTime.TotalSeconds;
            currentTime = SkipOverUnmovableBlocks(currentTime, unmovableBlocks);
            prevIndex = stopIndex;
        }

        return arrivalTimes;
    }

    private static double CalculateDepartureTimeAtPosition(
        int position,
        List<int> route,
        DistanceMatrix distanceMatrix,
        int startBaseIndex,
        double containerFromTimeSeconds,
        List<PlacedTimeBlock> placedBlocks)
    {
        var currentTime = containerFromTimeSeconds;
        var prevIndex = startBaseIndex;

        for (int i = 0; i < position && i < route.Count; i++)
        {
            currentTime += distanceMatrix.DurationMatrix[prevIndex, route[i]];
            currentTime += distanceMatrix.Locations[route[i]].TotalOnSiteTime.TotalSeconds;

            foreach (var block in placedBlocks.Where(b => b.InsertionPosition == i + 1).OrderBy(b => b.StartTimeSeconds))
            {
                if (currentTime < block.EndTimeSeconds)
                {
                    currentTime = block.EndTimeSeconds;
                }
            }

            prevIndex = route[i];
        }

        foreach (var block in placedBlocks.Where(b => b.InsertionPosition == position).OrderBy(b => b.StartTimeSeconds))
        {
            if (currentTime < block.EndTimeSeconds)
            {
                currentTime = block.EndTimeSeconds;
            }
        }

        return currentTime;
    }

    private static double CalculateNextRequiredTime(
        int position,
        List<int> route,
        DistanceMatrix distanceMatrix,
        int startBaseIndex,
        int endBaseIndex,
        double containerFromTimeSeconds,
        List<PlacedTimeBlock> placedBlocks)
    {
        if (position >= route.Count)
        {
            return double.MaxValue;
        }

        var prevIndex = position == 0 ? startBaseIndex : route[position - 1];
        var travelTime = distanceMatrix.DurationMatrix[prevIndex, route[position]];

        var departureTime = CalculateDepartureTimeAtPosition(
            position, route, distanceMatrix, startBaseIndex, containerFromTimeSeconds, placedBlocks);

        return departureTime + travelTime;
    }
}
