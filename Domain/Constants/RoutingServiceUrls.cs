// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Base URLs and endpoint paths of the external routing services (OSRM public server and OpenRouteService).
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class RoutingServiceUrls
{
    public const string OsrmBaseUrl = "https://router.project-osrm.org";
    public const string OsrmRouteDrivingUrl = $"{OsrmBaseUrl}/route/v1/driving";
    public const string OpenRouteServiceBaseUrl = "https://api.openrouteservice.org/v2";
    public const string OpenRouteServiceDirectionsDrivingUrl = $"{OpenRouteServiceBaseUrl}/directions/driving-car";
}
