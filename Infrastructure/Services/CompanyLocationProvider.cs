// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the configured company address (APP_ADDRESS_* settings) to a coordinate via geocoding,
/// caching the outcome so the hot greeting path stays cheap. The geocode call is hard-bounded by a
/// deadline that covers the geocoder's multi-step fallback chain, so a slow or contended Nominatim
/// degrades to "no location" instead of stalling.
/// @param settingsReader - reads the APP_ADDRESS_* company address settings
/// @param geocodingService - resolves the composed address to latitude/longitude
/// @param cache - stores the resolved coordinate (positive long, negative short) to avoid repeat lookups
/// </summary>

using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Settings;
using Microsoft.Extensions.Caching.Memory;
using SettingsConstants = Klacks.Api.Application.Constants.Settings;

namespace Klacks.Api.Infrastructure.Services;

public class CompanyLocationProvider : ICompanyLocationProvider
{
    private const string CacheKey = "company_location_coords";
    private const int GeocodeTimeoutMs = 6000;
    private static readonly TimeSpan PositiveCacheDuration = TimeSpan.FromHours(6);
    private static readonly TimeSpan NegativeCacheDuration = TimeSpan.FromHours(1);

    private readonly ISettingsReader _settingsReader;
    private readonly IGeocodingService _geocodingService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CompanyLocationProvider> _logger;

    public CompanyLocationProvider(
        ISettingsReader settingsReader,
        IGeocodingService geocodingService,
        IMemoryCache cache,
        ILogger<CompanyLocationProvider> logger)
    {
        _settingsReader = settingsReader;
        _geocodingService = geocodingService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<(double Latitude, double Longitude)?> GetCompanyLocationAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out (double Latitude, double Longitude)? cached))
        {
            return cached;
        }

        var street = await ReadSetting(SettingsConstants.APP_ADDRESS_ADDRESS);
        var zip = await ReadSetting(SettingsConstants.APP_ADDRESS_ZIP);
        var place = await ReadSetting(SettingsConstants.APP_ADDRESS_PLACE);
        var country = await ReadSetting(SettingsConstants.APP_ADDRESS_COUNTRY);

        if (string.IsNullOrWhiteSpace(zip) && string.IsNullOrWhiteSpace(place))
        {
            return CacheResult(null);
        }

        var coordinates = await TryGeocode(BuildAddress(street, zip, place), country, cancellationToken);
        return CacheResult(coordinates);
    }

    private async Task<(double Latitude, double Longitude)?> TryGeocode(string address, string country, CancellationToken cancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(GeocodeTimeoutMs);

        try
        {
            var (latitude, longitude) = await _geocodingService.GeocodeAddressAsync(address, country, linkedCts.Token);
            if (latitude.HasValue && longitude.HasValue)
            {
                return (latitude.Value, longitude.Value);
            }
        }
        catch (OperationCanceledException) when (linkedCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Company location geocoding timed out after {TimeoutMs} ms; falling back to no location.", GeocodeTimeoutMs);
        }

        return null;
    }

    private async Task<string> ReadSetting(string type)
    {
        var setting = await _settingsReader.GetSetting(type);
        return setting?.Value ?? string.Empty;
    }

    private static string BuildAddress(string street, string zip, string place)
    {
        var locality = $"{zip} {place}".Trim();
        return string.IsNullOrWhiteSpace(street) ? locality : $"{street}, {locality}";
    }

    private (double Latitude, double Longitude)? CacheResult((double Latitude, double Longitude)? coordinates)
    {
        _cache.Set(CacheKey, coordinates, coordinates.HasValue ? PositiveCacheDuration : NegativeCacheDuration);
        return coordinates;
    }
}
