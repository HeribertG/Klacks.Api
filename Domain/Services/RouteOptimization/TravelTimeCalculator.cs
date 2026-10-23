// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Static utility class for travel time and distance calculations (Haversine, mixed travel times, duration lookups).
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Services.Geo;

namespace Klacks.Api.Domain.Services.RouteOptimization;

public static class TravelTimeCalculator
{
    private const int MinRouteStops = 2;
    private const int SecondToLastOffset = 2;

    public static TimeSpan CalculateMixedTravelTime(
        List<int> route,
        DistanceMatrix distanceMatrix,
        ContainerTransportMode containerTransportMode)
    {
        if (containerTransportMode != ContainerTransportMode.Mix || distanceMatrix.DurationMatricesByProfile == null)
        {
            return CalculateTotalTravelTime(route, distanceMatrix.DurationMatrix);
        }

        double totalSeconds = 0.0;
        for (int i = 0; i < route.Count - 1; i++)
        {
            var fromIndex = route[i];
            var toIndex = route[i + 1];
            var toLocation = distanceMatrix.Locations[toIndex];
            var profile = GetOsrmProfileFromTransportMode(toLocation.TransportMode);

            if (distanceMatrix.DurationMatricesByProfile.TryGetValue(profile, out var durationMatrix))
            {
                var segmentDuration = durationMatrix[fromIndex, toIndex];
                totalSeconds += segmentDuration;
            }
            else
            {
                totalSeconds += distanceMatrix.DurationMatrix[fromIndex, toIndex];
            }
        }

        return TimeSpan.FromSeconds(totalSeconds);
    }

    public static TimeSpan GetMixedTravelTimeForSegment(
        int fromIndex,
        int toIndex,
        DistanceMatrix distanceMatrix,
        ContainerTransportMode containerTransportMode)
    {
        if (containerTransportMode != ContainerTransportMode.Mix || distanceMatrix.DurationMatricesByProfile == null)
        {
            return GetTravelTimeFromDuration(distanceMatrix.DurationMatrix, fromIndex, toIndex);
        }

        var toLocation = distanceMatrix.Locations[toIndex];
        var profile = GetOsrmProfileFromTransportMode(toLocation.TransportMode);

        if (distanceMatrix.DurationMatricesByProfile.TryGetValue(profile, out var durationMatrix))
        {
            return TimeSpan.FromSeconds(durationMatrix[fromIndex, toIndex]);
        }

        return GetTravelTimeFromDuration(distanceMatrix.DurationMatrix, fromIndex, toIndex);
    }

    public static TimeSpan CalculateTotalTravelTime(List<int> route, double[,] durationMatrix)
    {
        double totalSeconds = 0.0;
        for (int i = 0; i < route.Count - 1; i++)
        {
            totalSeconds += durationMatrix[route[i], route[i + 1]];
        }
        return TimeSpan.FromSeconds(totalSeconds);
    }

    public static (TimeSpan fromStart, TimeSpan toEnd) CalculateBaseTravelTimes(
        DistanceMatrix distanceMatrix, List<int> route, int? startIndex, int? endIndex,
        double distanceFromStart, double distanceToEnd, ContainerTransportMode transportMode)
    {
        TimeSpan travelTimeFromStartBase = TimeSpan.Zero;
        if (distanceFromStart > 0 && startIndex.HasValue && route.Count > 0)
        {
            travelTimeFromStartBase = GetMixedTravelTimeForSegment(startIndex.Value, route[0], distanceMatrix, transportMode);
        }

        TimeSpan travelTimeToEndBase = TimeSpan.Zero;
        if (distanceToEnd > 0 && endIndex.HasValue && route.Count > 0)
        {
            var lastRouteIndex = route.Last() != endIndex.Value ? route.Last() : (route.Count >= MinRouteStops ? route[route.Count - SecondToLastOffset] : route[0]);
            travelTimeToEndBase = GetMixedTravelTimeForSegment(lastRouteIndex, endIndex.Value, distanceMatrix, transportMode);
        }

        return (travelTimeFromStartBase, travelTimeToEndBase);
    }

    public static TimeSpan GetTravelTimeFromDuration(double[,] durationMatrix, int fromIndex, int toIndex)
    {
        var durationSeconds = durationMatrix[fromIndex, toIndex];
        return TimeSpan.FromSeconds(durationSeconds);
    }

    public static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        return HaversineDistanceCalculator.DistanceKm(lat1, lon1, lat2, lon2);
    }

    public static double CalculateTotalDistance(List<int> route, double[,] matrix)
    {
        double total = 0.0;
        for (int i = 0; i < route.Count - 1; i++)
        {
            total += matrix[route[i], route[i + 1]];
        }
        return total;
    }

    private static string GetOsrmProfileFromTransportMode(TransportMode transportMode)
    {
        return transportMode switch
        {
            TransportMode.ByCar => "driving",
            TransportMode.ByBicycle => "cycling",
            TransportMode.ByFoot => "foot",
            _ => "driving"
        };
    }
}
