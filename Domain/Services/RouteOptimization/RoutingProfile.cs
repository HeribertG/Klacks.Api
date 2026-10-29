// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The routing profiles Klacks asks OSRM and OpenRouteService for. An item transport mode travels with its own
/// profile (driving for a car and for any mode without its own profile). A container transport mode picks one profile
/// for the whole tour: car, bicycle and foot their own, mix the car profile, because a single route geometry or matrix
/// cannot switch profiles per leg.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.RouteOptimization;

public static class RoutingProfile
{
    public const string Driving = "driving";

    public const string Cycling = "cycling";

    public const string Foot = "foot";

    public const string OpenRouteServiceDrivingCar = "driving-car";

    public const string OpenRouteServiceCyclingRegular = "cycling-regular";

    public const string OpenRouteServiceFootWalking = "foot-walking";

    public static string FromTransportMode(TransportMode transportMode) => transportMode switch
    {
        TransportMode.ByBicycle => Cycling,
        TransportMode.ByFoot => Foot,
        _ => Driving
    };

    /// <summary>
    /// OSRM profile of a container tour.
    /// </summary>
    /// <param name="transportMode">Transport mode of the container template</param>
    public static string FromContainerTransportMode(ContainerTransportMode transportMode) => transportMode switch
    {
        ContainerTransportMode.ByBicycle => Cycling,
        ContainerTransportMode.ByFoot => Foot,
        ContainerTransportMode.ByCar => Driving,
        ContainerTransportMode.Mix => Driving,
        _ => Driving
    };

    /// <summary>
    /// OpenRouteService profile of a container tour (matrix and directions).
    /// </summary>
    /// <param name="transportMode">Transport mode of the container template</param>
    public static string OpenRouteServiceProfile(ContainerTransportMode transportMode) => transportMode switch
    {
        ContainerTransportMode.ByBicycle => OpenRouteServiceCyclingRegular,
        ContainerTransportMode.ByFoot => OpenRouteServiceFootWalking,
        ContainerTransportMode.ByCar => OpenRouteServiceDrivingCar,
        ContainerTransportMode.Mix => OpenRouteServiceDrivingCar,
        _ => OpenRouteServiceDrivingCar
    };

    /// <summary>
    /// OpenRouteService GeoJSON directions path of a container tour.
    /// </summary>
    /// <param name="transportMode">Transport mode of the container template</param>
    public static string OpenRouteServiceDirectionsGeoJsonPath(ContainerTransportMode transportMode) =>
        OpenRouteServiceProfile(transportMode) switch
        {
            OpenRouteServiceCyclingRegular => GeoServicePaths.OpenRouteServiceDirectionsCyclingRegularGeoJson,
            OpenRouteServiceFootWalking => GeoServicePaths.OpenRouteServiceDirectionsFootWalkingGeoJson,
            _ => GeoServicePaths.OpenRouteServiceDirectionsDrivingCarGeoJson
        };
}
