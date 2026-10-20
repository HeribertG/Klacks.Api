// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Position of a place derived from its addresses: the mean latitude and longitude of the addresses that
/// carry coordinates. Shared by the cluster planners so a place is measured the same way everywhere.
/// </summary>
/// <param name="Latitude">Mean latitude of the place's geocoded addresses.</param>
/// <param name="Longitude">Mean longitude of the place's geocoded addresses.</param>
/// <param name="LocatedAddressCount">Number of addresses that contributed coordinates.</param>

namespace Klacks.Api.Domain.Services.Geo;

public readonly record struct PlacePosition(double Latitude, double Longitude, int LocatedAddressCount)
{
    /// <param name="addresses">Addresses of one place; addresses without coordinates are ignored</param>
    /// <returns>The mean position, or null when no address of the place has coordinates</returns>
    public static PlacePosition? Of(IReadOnlyCollection<ClusterAddress> addresses)
    {
        ArgumentNullException.ThrowIfNull(addresses);

        var located = addresses.Where(a => a.Latitude.HasValue && a.Longitude.HasValue).ToList();
        if (located.Count == 0)
        {
            return null;
        }

        return new PlacePosition(
            located.Average(a => a.Latitude!.Value),
            located.Average(a => a.Longitude!.Value),
            located.Count);
    }
}
