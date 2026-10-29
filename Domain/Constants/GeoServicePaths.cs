// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Host-independent endpoint paths of the geo services (Nominatim, OSRM, OpenRouteService). They are appended to
/// the base URL that IGeoServicesPolicy resolves, so a self-hosted instance with the same API works unchanged.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class GeoServicePaths
{
    public const string NominatimSearch = "/search";
    public const string OsrmRoute = "/route/v1";
    public const string OsrmRouteDriving = "/route/v1/driving";
    public const string OsrmTableDriving = "/table/v1/driving";
    public const string OpenRouteServiceMatrix = "/matrix";
    public const string OpenRouteServiceDirectionsDrivingCar = "/directions/driving-car";
    public const string OpenRouteServiceDirectionsDrivingCarGeoJson = "/directions/driving-car/geojson";
    public const string OpenRouteServiceDirectionsCyclingRegularGeoJson = "/directions/cycling-regular/geojson";
    public const string OpenRouteServiceDirectionsFootWalkingGeoJson = "/directions/foot-walking/geojson";
}
