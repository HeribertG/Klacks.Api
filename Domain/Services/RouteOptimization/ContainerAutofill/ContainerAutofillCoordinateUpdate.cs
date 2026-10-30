// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

/// <summary>
/// Coordinates a geocoder found for an address that had none stored.
/// </summary>
/// <param name="AddressId">Address the coordinates belong to</param>
/// <param name="Latitude">Geocoded latitude</param>
/// <param name="Longitude">Geocoded longitude</param>
public sealed record ContainerAutofillCoordinateUpdate(Guid AddressId, double Latitude, double Longitude);
