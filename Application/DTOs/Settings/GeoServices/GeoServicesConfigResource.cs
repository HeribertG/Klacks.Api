// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Installation-wide geo service configuration for the UI (maps, address search, route display, admin status card).
/// Deliberately carries no geocoding, routing or OpenRouteService base URL, so internal host names never reach a
/// browser; only the map tile URL template is included because the browser loads the tiles itself.
/// </summary>

namespace Klacks.Api.Application.DTOs.Settings.GeoServices;

public class GeoServicesConfigResource
{
    public bool NoOutboundTraffic { get; set; }

    public GeoServiceStatusResource Geocoding { get; set; } = new();

    public GeoServiceStatusResource Routing { get; set; } = new();

    public GeoServiceStatusResource OpenRouteService { get; set; } = new();

    public MapTileConfigResource MapTiles { get; set; } = new();

    public bool ExternalMapLinksEnabled { get; set; }
}
