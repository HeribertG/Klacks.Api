// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Berechnet Reisezeiten zwischen Einsatzorten via OpenRouteService, OSRM oder Haversine-Fallback.
/// Nutzt IGeocodingService für Koordinaten-Auflösung und cached API-Key-Status für 5 Minuten.
/// </summary>
/// <param name="settingsRepository">Zugriff auf App-Einstellungen (API-Key)</param>
/// <param name="encryptionService">Entschlüsselung sensibler Einstellungen</param>
/// <param name="geocodingService">Koordinaten-Auflösung für Adressen ohne Lat/Lon</param>
using System.Text.Json;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using AppSettings = Klacks.Api.Application.Constants.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services;

public class TravelTimeCalculationService : ITravelTimeCalculationService
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ISettingsEncryptionService _encryptionService;
    private readonly IGeocodingService _geocodingService;
    private readonly IMemoryCache _cache;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TravelTimeCalculationService> _logger;

    private const string OPENROUTE_DIRECTIONS_URL = "https://api.openrouteservice.org/v2/directions/driving-car";
    private const string OSRM_ROUTE_URL = "http://router.project-osrm.org/route/v1/driving";
    private const string API_KEY_CACHE_KEY = "travel_time_api_key_configured";
    private const string TRAVEL_TIME_CACHE_PREFIX = "travel_time_";
    private const double AVERAGE_SPEED_KMH = 40.0;
    private const double EARTH_RADIUS_KM = 6371.0;

    public TravelTimeCalculationService(
        ISettingsRepository settingsRepository,
        ISettingsEncryptionService encryptionService,
        IGeocodingService geocodingService,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory,
        ILogger<TravelTimeCalculationService> logger)
    {
        _settingsRepository = settingsRepository;
        _encryptionService = encryptionService;
        _geocodingService = geocodingService;
        _cache = cache;
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    public async Task<bool> IsApiKeyConfiguredAsync()
    {
        if (_cache.TryGetValue(API_KEY_CACHE_KEY, out bool configured))
        {
            return configured;
        }

        var apiKey = await GetApiKeyAsync();
        configured = !string.IsNullOrWhiteSpace(apiKey);
        _cache.Set(API_KEY_CACHE_KEY, configured, TimeSpan.FromMinutes(5));
        return configured;
    }

    public async Task<TimeSpan?> CalculateTravelTimeAsync(Address from, Address to, CancellationToken ct)
    {
        var fromCoords = await GetCoordinatesAsync(from);
        var toCoords = await GetCoordinatesAsync(to);

        if (fromCoords == null || toCoords == null)
        {
            return null;
        }

        var cacheKey = $"{TRAVEL_TIME_CACHE_PREFIX}{fromCoords.Value.Lat:F5}_{fromCoords.Value.Lon:F5}_{toCoords.Value.Lat:F5}_{toCoords.Value.Lon:F5}";
        if (_cache.TryGetValue(cacheKey, out TimeSpan cachedDuration))
        {
            return cachedDuration;
        }

        TimeSpan? result = null;

        var apiKey = await GetApiKeyAsync();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            result = await TryOpenRouteServiceAsync(fromCoords.Value, toCoords.Value, apiKey, ct);
        }

        result ??= await TryOsrmAsync(fromCoords.Value, toCoords.Value, ct);
        result ??= CalculateHaversineTravelTime(fromCoords.Value, toCoords.Value);

        if (result.HasValue)
        {
            _cache.Set(cacheKey, result.Value, TimeSpan.FromDays(30));
        }

        return result;
    }

    private async Task<string?> GetApiKeyAsync()
    {
        var setting = await _settingsRepository.GetSetting(AppSettings.OPENROUTESERVICE_API_KEY);
        if (setting == null || string.IsNullOrEmpty(setting.Value))
        {
            return null;
        }

        return _encryptionService.ProcessForReading(setting.Type, setting.Value);
    }

    private async Task<(double Lat, double Lon)?> GetCoordinatesAsync(Address address)
    {
        if (address.Latitude.HasValue && address.Longitude.HasValue)
        {
            return (address.Latitude.Value, address.Longitude.Value);
        }

        var fullAddress = BuildFullAddress(address);
        if (string.IsNullOrWhiteSpace(fullAddress))
        {
            return null;
        }

        var (lat, lon) = await _geocodingService.GeocodeAddressAsync(fullAddress, address.Country);
        if (lat.HasValue && lon.HasValue)
        {
            return (lat.Value, lon.Value);
        }

        return null;
    }

    private static string BuildFullAddress(Address address)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(address.Street))
        {
            parts.Add(address.Street);
        }

        var zipCity = $"{address.Zip} {address.City}".Trim();
        if (!string.IsNullOrWhiteSpace(zipCity))
        {
            parts.Add(zipCity);
        }

        return string.Join(", ", parts);
    }

    private async Task<TimeSpan?> TryOpenRouteServiceAsync(
        (double Lat, double Lon) from,
        (double Lat, double Lon) to,
        string apiKey,
        CancellationToken ct)
    {
        try
        {
            var url = $"{OPENROUTE_DIRECTIONS_URL}?start={from.Lon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{from.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&end={to.Lon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{to.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", apiKey);

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenRouteService request failed with status {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var features = doc.RootElement.GetProperty("features");
            if (features.GetArrayLength() == 0)
            {
                return null;
            }

            var duration = features[0]
                .GetProperty("properties")
                .GetProperty("summary")
                .GetProperty("duration")
                .GetDouble();

            return TimeSpan.FromSeconds(duration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenRouteService travel time calculation failed");
            return null;
        }
    }

    private async Task<TimeSpan?> TryOsrmAsync(
        (double Lat, double Lon) from,
        (double Lat, double Lon) to,
        CancellationToken ct)
    {
        try
        {
            var url = $"{OSRM_ROUTE_URL}/{from.Lon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{from.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture)};{to.Lon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{to.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}?overview=false";

            using var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OSRM request failed with status {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var routes = doc.RootElement.GetProperty("routes");
            if (routes.GetArrayLength() == 0)
            {
                return null;
            }

            var duration = routes[0].GetProperty("duration").GetDouble();
            return TimeSpan.FromSeconds(duration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OSRM travel time calculation failed");
            return null;
        }
    }

    private static TimeSpan CalculateHaversineTravelTime(
        (double Lat, double Lon) from,
        (double Lat, double Lon) to)
    {
        var dLat = DegreesToRadians(to.Lat - from.Lat);
        var dLon = DegreesToRadians(to.Lon - from.Lon);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(from.Lat)) * Math.Cos(DegreesToRadians(to.Lat)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distanceKm = EARTH_RADIUS_KM * c;

        return TimeSpan.FromHours(distanceKm / AVERAGE_SPEED_KMH);
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
