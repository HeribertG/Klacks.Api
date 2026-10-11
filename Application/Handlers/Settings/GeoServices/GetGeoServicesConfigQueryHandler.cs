// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Projects the startup-resolved geo service policy onto the UI resource: on/off and reason per service, the map
/// tile source and whether external map links may be shown. Base URLs of geocoding, routing and OpenRouteService
/// are never copied, so internal host names stay on the server.
/// </summary>
/// <param name="geoServicesPolicy">Effective geo service configuration, resolved once from environment variables</param>

using Klacks.Api.Application.DTOs.Settings.GeoServices;
using Klacks.Api.Application.Queries.Settings.GeoServices;
using Klacks.Api.Domain.Interfaces.GeoServices;
using Klacks.Api.Domain.Models.Settings.GeoServices;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Settings.GeoServices;

public class GetGeoServicesConfigQueryHandler : IRequestHandler<GetGeoServicesConfigQuery, GeoServicesConfigResource>
{
    private readonly IGeoServicesPolicy _geoServicesPolicy;

    public GetGeoServicesConfigQueryHandler(IGeoServicesPolicy geoServicesPolicy)
    {
        _geoServicesPolicy = geoServicesPolicy;
    }

    public Task<GeoServicesConfigResource> Handle(GetGeoServicesConfigQuery request, CancellationToken cancellationToken)
    {
        var mapTiles = _geoServicesPolicy.MapTiles;

        return Task.FromResult(new GeoServicesConfigResource
        {
            NoOutboundTraffic = _geoServicesPolicy.NoOutboundTraffic,
            Geocoding = ToStatus(_geoServicesPolicy.Geocoding),
            Routing = ToStatus(_geoServicesPolicy.Routing),
            OpenRouteService = ToStatus(_geoServicesPolicy.OpenRouteService),
            MapTiles = new MapTileConfigResource
            {
                Enabled = mapTiles.IsEnabled,
                Reason = mapTiles.DisabledReason,
                UrlTemplate = mapTiles.IsEnabled ? mapTiles.UrlTemplate : null,
                Subdomains = mapTiles.Subdomains.ToList(),
                Attribution = mapTiles.Attribution,
                MaxZoom = mapTiles.MaxZoom,
            },
            ExternalMapLinksEnabled = _geoServicesPolicy.ExternalMapLinksEnabled,
        });
    }

    private static GeoServiceStatusResource ToStatus(GeoServiceEndpoint endpoint)
    {
        return new GeoServiceStatusResource
        {
            Enabled = endpoint.IsEnabled,
            Reason = endpoint.DisabledReason,
        };
    }
}
