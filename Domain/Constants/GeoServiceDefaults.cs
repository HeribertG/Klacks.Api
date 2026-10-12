// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The only place in Klacks.Api that names the public OpenStreetMap-based geo service hosts: the defaults used
/// when an operator configures no own instance, and the list of public hosts that the NoOutboundTraffic switch
/// blocks even when an operator enters one of them as an "own" URL. Guarded by ExternalGeoServiceUrlGuardTests.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class GeoServiceDefaults
{
    public const string NominatimBaseUrl = "https://nominatim.openstreetmap.org";
    public const string OsrmBaseUrl = "https://router.project-osrm.org";
    public const string OpenRouteServiceBaseUrl = "https://api.openrouteservice.org/v2";
    public const string OsmTileUrlTemplate = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
    public const string OsmTileAttribution = "© OpenStreetMap contributors";
    public const int OsmTileMaxZoom = 19;
    public const string OsmTileCspSources = "https://tile.openstreetmap.org https://*.tile.openstreetmap.org";

    public static readonly IReadOnlyList<string> PublicHostDomains =
    [
        "openstreetmap.org",
        "openstreetmap.de",
        "project-osrm.org",
        "openrouteservice.org",
        "basemaps.cartocdn.com",
        "opentopomap.org"
    ];
}
