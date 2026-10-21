// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared geocoding for groups: resolves a place name to coordinates via the configured geocoder,
/// using the company default country (APP_ADDRESS_COUNTRY) named like every other geocoding query; without a
/// configured default country no country is forced on the query. Single place both the manual set_group_location
/// skill and the automatic GroupLocationResolver call, so the "name -> coordinates" step never diverges.
/// </summary>

using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Common;

namespace Klacks.Api.Application.Services.Grouping;

public class GroupGeocoder : IGroupGeocoder
{
    private readonly IGeocodingService _geocodingService;
    private readonly ICountryResolver _countryResolver;

    public GroupGeocoder(IGeocodingService geocodingService, ICountryResolver countryResolver)
    {
        _geocodingService = geocodingService;
        _countryResolver = countryResolver;
    }

    public bool IsEnabled => _geocodingService.IsEnabled;

    public async Task<(double? Latitude, double? Longitude)> GeocodeAsync(
        string placeName, CancellationToken cancellationToken = default)
    {
        var country = await _countryResolver.ResolveDefaultGeocodingNameAsync(cancellationToken);
        return await _geocodingService.GeocodeAsync(placeName, country, cancellationToken);
    }
}
