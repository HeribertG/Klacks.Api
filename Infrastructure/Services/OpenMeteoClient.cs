// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Thin wrapper around the public Open-Meteo current-weather endpoint. Maps the WMO weather code
/// to one of Klacksy's weather i18n keys. Results are cached in-memory per coarsely-rounded
/// coordinate and hour so concurrent users near the same spot don't spam the upstream API.
/// </summary>

namespace Klacks.Api.Infrastructure.Services;

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.Extensions.Caching.Memory;

public sealed class OpenMeteoClient : IOpenMeteoClient
{
    public const string HttpClientName = "OpenMeteo";

    private const string EndpointTemplate = "v1/forecast?latitude={0}&longitude={1}&current_weather=true&timezone=auto";
    private const int CoordinateDecimals = 1;
    private const int CacheMinutes = 60;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OpenMeteoClient> _logger;

    public OpenMeteoClient(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<OpenMeteoClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> GetWeatherKeyAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(latitude, longitude);
        if (_cache.TryGetValue<string>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var url = string.Format(
                CultureInfo.InvariantCulture,
                EndpointTemplate,
                latitude.ToString("0.0####", CultureInfo.InvariantCulture),
                longitude.ToString("0.0####", CultureInfo.InvariantCulture));

            var response = await client.GetFromJsonAsync<OpenMeteoResponse>(url, JsonOptions, cancellationToken);
            var key = MapWeatherCode(response?.CurrentWeather?.WeatherCode);

            _cache.Set(cacheKey, key, TimeSpan.FromMinutes(CacheMinutes));
            return key;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            _logger.LogDebug(ex, "Open-Meteo lookup failed for ({Lat}, {Lon}) — greeting will skip weather.", latitude, longitude);
            return string.Empty;
        }
    }

    private static string BuildCacheKey(double latitude, double longitude)
    {
        var lat = Math.Round(latitude, CoordinateDecimals, MidpointRounding.AwayFromZero);
        var lon = Math.Round(longitude, CoordinateDecimals, MidpointRounding.AwayFromZero);
        var hour = DateTime.UtcNow.ToString("yyyyMMddHH", CultureInfo.InvariantCulture);
        return $"{HttpClientName}|{lat}|{lon}|{hour}";
    }

    private static string MapWeatherCode(int? code) => code switch
    {
        0 => WelcomeI18nKeys.Weather.Clear,
        1 => WelcomeI18nKeys.Weather.Sunny,
        2 => WelcomeI18nKeys.Weather.Cloudy,
        3 => WelcomeI18nKeys.Weather.Overcast,
        45 or 48 => WelcomeI18nKeys.Weather.Fog,
        51 or 53 or 55 or 56 or 57 => WelcomeI18nKeys.Weather.Drizzle,
        61 or 63 or 65 or 66 or 67 or 80 or 81 or 82 => WelcomeI18nKeys.Weather.Rainy,
        71 or 73 or 75 or 77 or 85 or 86 => WelcomeI18nKeys.Weather.Snowy,
        95 or 96 or 99 => WelcomeI18nKeys.Weather.Thunder,
        _ => WelcomeI18nKeys.Weather.Stormy,
    };

    private sealed class OpenMeteoResponse
    {
        [JsonPropertyName("current_weather")]
        public CurrentWeatherDto? CurrentWeather { get; set; }
    }

    private sealed class CurrentWeatherDto
    {
        [JsonPropertyName("weathercode")]
        public int? WeatherCode { get; set; }
    }
}
