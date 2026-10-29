// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Turns an optimizer result into route steps with the distance and travel time of each leg. The route optimization
/// takes the leg time from the routing profile of the next stop's transport mode when the container uses the mix
/// mode; the container autofill always takes the plain duration matrix. Both rules are kept as they were in the
/// route optimization endpoint.
/// </summary>
/// <param name="result">Optimizer result whose full route (with start and end base) becomes the steps</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.RouteOptimization.VisitScheduling;

public static class RouteStepBuilder
{
    public static List<RouteStep> FromOptimization(RouteOptimizationResult result)
    {
        return Build(
            result.OptimizedRoute,
            result.FullRouteIndices,
            result.DistanceMatrix,
            (fromIndex, toIndex, mode) => SegmentTravelTime(result, fromIndex, toIndex, mode));
    }

    public static List<RouteStep> FromAutofill(ContainerAutofillResult result)
    {
        return Build(
            result.OptimizedRoute,
            result.FullRouteIndices,
            result.DistanceMatrix,
            (fromIndex, toIndex, _) => TimeSpan.FromSeconds(result.DurationMatrix[fromIndex, toIndex]));
    }

    private static List<RouteStep> Build(
        List<Location> route,
        List<int> fullRouteIndices,
        double[,] distanceMatrix,
        Func<int, int, TransportMode, TimeSpan> legTravelTime)
    {
        var steps = new List<RouteStep>(route.Count);
        for (var i = 0; i < route.Count; i++)
        {
            var location = route[i];
            double distanceToNext = 0;
            var travelTimeToNext = TimeSpan.Zero;

            if (i < route.Count - 1 && i + 1 < fullRouteIndices.Count)
            {
                var currentIndex = fullRouteIndices[i];
                var nextIndex = fullRouteIndices[i + 1];
                distanceToNext = distanceMatrix[currentIndex, nextIndex];
                travelTimeToNext = legTravelTime(currentIndex, nextIndex, route[i + 1].TransportMode);
            }

            steps.Add(new RouteStep(
                i + 1,
                location.Name,
                location.Address,
                location.Latitude,
                location.Longitude,
                location.ShiftId,
                location.TransportMode,
                distanceToNext,
                travelTimeToNext));
        }

        return steps;
    }

    private static TimeSpan SegmentTravelTime(
        RouteOptimizationResult result, int fromIndex, int toIndex, TransportMode segmentTransportMode)
    {
        if (result.TransportMode != ContainerTransportMode.Mix || result.DurationMatricesByProfile == null)
        {
            return TimeSpan.FromSeconds(result.DurationMatrix[fromIndex, toIndex]);
        }

        var profile = RoutingProfile.FromTransportMode(segmentTransportMode);

        return result.DurationMatricesByProfile.TryGetValue(profile, out var durationMatrix)
            ? TimeSpan.FromSeconds(durationMatrix[fromIndex, toIndex])
            : TimeSpan.FromSeconds(result.DurationMatrix[fromIndex, toIndex]);
    }
}
