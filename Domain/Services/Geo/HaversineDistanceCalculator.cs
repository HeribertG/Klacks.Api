// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Computes the great-circle distance between two WGS84 coordinates using the Haversine formula.
/// </summary>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Geo;

public static class HaversineDistanceCalculator
{
    /// <summary>
    /// Great-circle distance in kilometres between two latitude/longitude points (degrees).
    /// </summary>
    /// <param name="latitude1">Latitude of the first point in degrees.</param>
    /// <param name="longitude1">Longitude of the first point in degrees.</param>
    /// <param name="latitude2">Latitude of the second point in degrees.</param>
    /// <param name="longitude2">Longitude of the second point in degrees.</param>
    public static double DistanceKm(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        var deltaLat = ToRadians(latitude2 - latitude1);
        var deltaLon = ToRadians(longitude2 - longitude1);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2)
                + Math.Cos(ToRadians(latitude1)) * Math.Cos(ToRadians(latitude2))
                  * Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return GeoConstants.EarthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
