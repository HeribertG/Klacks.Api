// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared geocoding for groups: resolves a place name to coordinates via the configured geocoder,
/// using the system default country. Single place both the manual set_group_location skill and the
/// automatic GroupLocationResolver call, so the "name -> coordinates" step never diverges.
/// </summary>

using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Application.Services.Grouping;

public class GroupGeocoder : IGroupGeocoder
{
    private const string DefaultCountryName = "Switzerland";

    private readonly IGeocodingService _geocodingService;
    private readonly ICountryResolver _countryResolver;

    public GroupGeocoder(IGeocodingService geocodingService, ICountryResolver countryResolver)
    {
        _geocodingService = geocodingService;
        _countryResolver = countryResolver;
    }

    public async Task<(double? Latitude, double? Longitude)> GeocodeAsync(
        string placeName, CancellationToken cancellationToken = default)
    {
        var country = await ResolveCountryNameAsync(cancellationToken);
        return await _geocodingService.GeocodeAsync(placeName, country, cancellationToken);
    }

    private async Task<string> ResolveCountryNameAsync(CancellationToken cancellationToken)
    {
        var country = await _countryResolver.GetDefaultAsync(cancellationToken);
        return country?.Name.En
               ?? country?.Name.De
               ?? country?.Abbreviation
               ?? DefaultCountryName;
    }
}
