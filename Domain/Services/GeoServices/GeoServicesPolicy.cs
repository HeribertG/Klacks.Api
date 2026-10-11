// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Singleton view of the effective geo service configuration, computed once from the bound GeoServices options
/// (environment only, changes need a restart). Options that cannot be bound at all (e.g. "ture" for a bool)
/// switch every geo service off with InvalidConfiguration instead of crashing the host.
/// </summary>
/// <param name="options">GeoServices options bound from configuration</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.GeoServices;
using Klacks.Api.Domain.Models.Settings.GeoServices;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Domain.Services.GeoServices;

public class GeoServicesPolicy : IGeoServicesPolicy
{
    public GeoServicesPolicy(IOptions<GeoServicesOptions> options)
    {
        GeoServicesOptions? value;
        try
        {
            value = options.Value;
        }
        catch (InvalidOperationException)
        {
            value = null;
        }

        if (value == null)
        {
            NoOutboundTraffic = true;
            var invalid = new GeoServiceEndpoint(false, null, GeoServiceDisabledReason.InvalidConfiguration);
            Geocoding = invalid;
            Routing = invalid;
            OpenRouteService = invalid;
            MapTiles = new MapTileEndpoint(
                false, null, [], GeoServiceDefaults.OsmTileAttribution, GeoServiceDefaults.OsmTileMaxZoom,
                GeoServiceDisabledReason.InvalidConfiguration);
            return;
        }

        NoOutboundTraffic = value.NoOutboundTraffic;
        Geocoding = GeoServicesPolicyEvaluator.Evaluate(value.Geocoding, NoOutboundTraffic, GeoServiceDefaults.NominatimBaseUrl);
        Routing = GeoServicesPolicyEvaluator.Evaluate(value.Routing, NoOutboundTraffic, GeoServiceDefaults.OsrmBaseUrl);
        OpenRouteService = GeoServicesPolicyEvaluator.Evaluate(
            value.OpenRouteService, NoOutboundTraffic, GeoServiceDefaults.OpenRouteServiceBaseUrl);
        MapTiles = GeoServicesPolicyEvaluator.EvaluateMapTiles(value.MapTiles, NoOutboundTraffic);
    }

    public bool NoOutboundTraffic { get; }

    public GeoServiceEndpoint Geocoding { get; }

    public GeoServiceEndpoint Routing { get; }

    public GeoServiceEndpoint OpenRouteService { get; }

    public MapTileEndpoint MapTiles { get; }

    public bool ExternalMapLinksEnabled => !NoOutboundTraffic;

    public GeoServiceEndpoint Get(GeoServiceKind kind) => kind switch
    {
        GeoServiceKind.Geocoding => Geocoding,
        GeoServiceKind.Routing => Routing,
        GeoServiceKind.OpenRouteService => OpenRouteService,
        GeoServiceKind.MapTiles => new GeoServiceEndpoint(MapTiles.IsEnabled, MapTiles.UrlTemplate, MapTiles.DisabledReason),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown geo service kind.")
    };
}
