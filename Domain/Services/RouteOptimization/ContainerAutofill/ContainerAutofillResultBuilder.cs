// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the container autofill result from a planned route: full route over the bases, distances, travel and
/// on-site time, remaining budget and the placed time blocks.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

public static class ContainerAutofillResultBuilder
{
    private const int TotalDistanceDecimals = 2;

    public static ContainerAutofillResult BuildResult(
        ContainerAutofillRouteContext context,
        List<int> optimizedRoute,
        int totalAvailableShifts,
        ContainerTransportMode transportMode,
        List<TimeBlock>? allTimeBlocks = null)
    {
        var matrix = context.DistanceMatrix;
        var selectedShiftIds = optimizedRoute
            .Select(i => matrix.Locations[i].ShiftId)
            .Where(id => id != Guid.Empty)
            .ToList();

        var fullRouteIndices = new List<int> { context.StartBaseIndex };
        fullRouteIndices.AddRange(optimizedRoute);
        fullRouteIndices.Add(context.EndBaseIndex);
        var fullRoute = fullRouteIndices.Select(idx => matrix.Locations[idx]).ToList();

        var hasStops = optimizedRoute.Count > 0;
        double distanceFromStartBase = hasStops ? matrix.Matrix[context.StartBaseIndex, optimizedRoute[0]] : 0;
        double distanceToEndBase = hasStops ? matrix.Matrix[optimizedRoute.Last(), context.EndBaseIndex] : 0;
        var travelTimeFromStartBase = hasStops
            ? TimeSpan.FromSeconds(matrix.DurationMatrix[context.StartBaseIndex, optimizedRoute[0]])
            : TimeSpan.Zero;
        var travelTimeToEndBase = hasStops
            ? TimeSpan.FromSeconds(matrix.DurationMatrix[optimizedRoute.Last(), context.EndBaseIndex])
            : TimeSpan.Zero;

        var totalWorkTime = SumOnSiteTime(matrix, optimizedRoute);
        var timeBudget = TimeSpan.FromSeconds(context.BudgetSeconds);
        var estimatedTravelTime = TimeSpan.FromSeconds(SumTravelSeconds(context, optimizedRoute)) + totalWorkTime;
        var remainingTime = timeBudget - estimatedTravelTime;

        var placedTimeBlocks = TimeBlockScheduler.PlaceAllBlocks(
            allTimeBlocks, optimizedRoute, matrix,
            context.StartBaseIndex, context.EndBaseIndex, context.ContainerFromTimeSeconds);

        return new ContainerAutofillResult(
            fullRoute,
            selectedShiftIds,
            Math.Round(SumDistance(matrix, fullRouteIndices), TotalDistanceDecimals),
            estimatedTravelTime,
            totalWorkTime,
            remainingTime > TimeSpan.Zero ? remainingTime : TimeSpan.Zero,
            totalAvailableShifts,
            selectedShiftIds.Count,
            matrix.Matrix,
            matrix.DurationMatrix,
            travelTimeFromStartBase,
            optimizedRoute,
            distanceFromStartBase,
            distanceToEndBase,
            travelTimeToEndBase,
            fullRouteIndices,
            matrix.DurationMatricesByProfile,
            transportMode,
            TotalBriefingDebriefingTime: totalWorkTime,
            PlacedTimeBlocks: placedTimeBlocks);
    }

    public static ContainerAutofillResult CreateEmptyResult(
        ContainerTransportMode transportMode,
        int totalAvailableShifts = 0)
    {
        return new ContainerAutofillResult(
            new List<Location>(),
            new List<Guid>(),
            0, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero,
            totalAvailableShifts, 0,
            new double[0, 0], new double[0, 0],
            TimeSpan.Zero, new List<int>(), 0, 0, TimeSpan.Zero,
            new List<int>(),
            TransportMode: transportMode);
    }

    private static double SumDistance(DistanceMatrix matrix, List<int> fullRouteIndices)
    {
        double totalDistance = 0;
        for (int i = 0; i < fullRouteIndices.Count - 1; i++)
        {
            totalDistance += matrix.Matrix[fullRouteIndices[i], fullRouteIndices[i + 1]];
        }

        return totalDistance;
    }

    private static double SumTravelSeconds(ContainerAutofillRouteContext context, List<int> optimizedRoute)
    {
        var durations = context.DistanceMatrix.DurationMatrix;
        double totalTravelSeconds = 0;
        if (optimizedRoute.Count > 0)
        {
            totalTravelSeconds += durations[context.StartBaseIndex, optimizedRoute[0]];
            for (int i = 0; i < optimizedRoute.Count - 1; i++)
            {
                totalTravelSeconds += durations[optimizedRoute[i], optimizedRoute[i + 1]];
            }
            totalTravelSeconds += durations[optimizedRoute.Last(), context.EndBaseIndex];
        }

        return totalTravelSeconds;
    }

    private static TimeSpan SumOnSiteTime(DistanceMatrix matrix, List<int> optimizedRoute)
    {
        var totalWorkTime = TimeSpan.Zero;
        foreach (var idx in optimizedRoute)
        {
            totalWorkTime += matrix.Locations[idx].TotalOnSiteTime;
        }

        return totalWorkTime;
    }
}
