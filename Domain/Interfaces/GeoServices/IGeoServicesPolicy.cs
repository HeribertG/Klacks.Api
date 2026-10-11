// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Effective, startup-resolved configuration of the external geo services (geocoding, OSRM routing,
/// OpenRouteService, map tiles). Every caller asks here whether a service may be called and where it lives;
/// a disabled service must not be called at all.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Settings.GeoServices;

namespace Klacks.Api.Domain.Interfaces.GeoServices;

public interface IGeoServicesPolicy
{
    bool NoOutboundTraffic { get; }

    GeoServiceEndpoint Geocoding { get; }

    GeoServiceEndpoint Routing { get; }

    GeoServiceEndpoint OpenRouteService { get; }

    MapTileEndpoint MapTiles { get; }

    bool ExternalMapLinksEnabled { get; }

    GeoServiceEndpoint Get(GeoServiceKind kind);
}
