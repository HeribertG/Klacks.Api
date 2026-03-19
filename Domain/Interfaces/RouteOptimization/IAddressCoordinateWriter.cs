// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Schreibt Geocoding-Koordinaten zurueck in die Address-Tabelle.
/// </summary>
/// <param name="addressId">ID der zu aktualisierenden Adresse</param>
/// <param name="latitude">Breitengrad</param>
/// <param name="longitude">Laengengrad</param>

namespace Klacks.Api.Domain.Interfaces.RouteOptimization;

public interface IAddressCoordinateWriter
{
    Task UpdateCoordinatesAsync(Guid addressId, double latitude, double longitude, CancellationToken cancellationToken = default);
}
