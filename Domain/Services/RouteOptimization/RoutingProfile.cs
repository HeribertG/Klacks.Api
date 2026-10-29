// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The OSRM routing profiles Klacks asks for and the profile each item transport mode travels with (driving for a
/// car and for any mode without its own profile).
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.RouteOptimization;

public static class RoutingProfile
{
    public const string Driving = "driving";

    public const string Cycling = "cycling";

    public const string Foot = "foot";

    public static string FromTransportMode(TransportMode transportMode) => transportMode switch
    {
        TransportMode.ByBicycle => Cycling,
        TransportMode.ByFoot => Foot,
        _ => Driving
    };
}
