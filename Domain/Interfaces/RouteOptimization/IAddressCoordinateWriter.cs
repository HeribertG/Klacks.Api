// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Writes geocoding coordinates back to the address table.
/// </summary>
/// <param name="addressId">ID of the address to update</param>
/// <param name="latitude">Latitude</param>
/// <param name="longitude">Longitude</param>

namespace Klacks.Api.Domain.Interfaces.RouteOptimization;

public interface IAddressCoordinateWriter
{
    Task UpdateCoordinatesAsync(Guid addressId, double latitude, double longitude, CancellationToken cancellationToken = default);
}
