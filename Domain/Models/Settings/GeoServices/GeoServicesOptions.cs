// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Settings.GeoServices;

public class GeoServicesOptions
{
    public const string SectionName = "GeoServices";

    public bool NoOutboundTraffic { get; set; }

    public GeoServiceEndpointOptions Geocoding { get; set; } = new();

    public GeoServiceEndpointOptions Routing { get; set; } = new();

    public GeoServiceEndpointOptions OpenRouteService { get; set; } = new();

    public MapTileServiceOptions MapTiles { get; set; } = new();
}
