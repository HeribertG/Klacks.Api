// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Static utility class for travel time and distance calculations (Haversine, mixed travel times, duration lookups).
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.RouteOptimization;

public static class TravelTimeCalculator
{
    private const double EARTH_RADIUS_KM = 6371.0;

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
            var lastRouteIndex = route.Last() != endIndex.Value ? route.Last() : (route.Count >= 2 ? route[route.Count - 2] : route[0]);
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
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EARTH_RADIUS_KM * c;
    }

    public static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
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
