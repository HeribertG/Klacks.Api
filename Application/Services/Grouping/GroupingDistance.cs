// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Great-circle distance in kilometres, used only as a tie-breaker between otherwise equal candidates.
/// Missing coordinates on either side count as infinitely far so that located candidates win.
/// </summary>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.Grouping;

public static class GroupingDistance
{
    private const double HalfCircleDegrees = 180.0;
    private const double Two = 2.0;

    public static double Kilometers(double? latitudeA, double? longitudeA, double? latitudeB, double? longitudeB)
    {
        if (latitudeA is not double latA || longitudeA is not double lonA
            || latitudeB is not double latB || longitudeB is not double lonB)
        {
            return double.PositiveInfinity;
        }

        var dLat = ToRadians(latB - latA);
        var dLon = ToRadians(lonB - lonA);
        var a = Math.Pow(Math.Sin(dLat / Two), Two)
            + Math.Cos(ToRadians(latA)) * Math.Cos(ToRadians(latB)) * Math.Pow(Math.Sin(dLon / Two), Two);
        return Two * GroupingFeasibilityDefaults.EarthRadiusKilometers * Math.Asin(Math.Sqrt(a));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / HalfCircleDegrees;
}
