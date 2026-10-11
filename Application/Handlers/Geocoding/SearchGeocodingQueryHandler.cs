// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Free-text place search through the server-side geocoder, replacing the browser's direct Nominatim call. A disabled
/// or unreachable geocoder is answered with a status instead of an error, so the UI can tell "off" from "no match".
/// </summary>
/// <param name="geocodingService">Geocoder honouring the geo service policy, its rate limit and its cache</param>

using Klacks.Api.Application.DTOs.Geocoding;
using Klacks.Api.Application.Queries.Geocoding;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Geocoding;

public class SearchGeocodingQueryHandler : IRequestHandler<SearchGeocodingQuery, GeocodingSearchResource>
{
    private readonly IGeocodingService _geocodingService;

    public SearchGeocodingQueryHandler(IGeocodingService geocodingService)
    {
        _geocodingService = geocodingService;
    }

    public async Task<GeocodingSearchResource> Handle(SearchGeocodingQuery request, CancellationToken cancellationToken)
    {
        var language = string.IsNullOrWhiteSpace(request.Language) ? null : request.Language.Trim();
        var result = await _geocodingService.SearchAsync(request.Query.Trim(), language, cancellationToken);

        return new GeocodingSearchResource
        {
            Status = result.Status,
            Latitude = result.Latitude,
            Longitude = result.Longitude,
            DisplayName = result.DisplayName,
        };
    }
}
