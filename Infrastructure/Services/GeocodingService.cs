using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Klacks.Api.Infrastructure.Services;

public interface IGeocodingService
{
    Task<(double? Latitude, double? Longitude)> GeocodeAsync(string city, string country);
}

public class GeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GeocodingService> _logger;
    private readonly SemaphoreSlim _rateLimiter;
    private const string NOMINATIM_URL = "https://nominatim.openstreetmap.org/search";
    private const int REQUEST_DELAY_MS = 1100;

    public GeocodingService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<GeocodingService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Nominatim");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Klacks Application (contact: admin@klacks.com)");
        _cache = cache;
        _logger = logger;
        _rateLimiter = new SemaphoreSlim(1, 1);
    }

    public async Task<(double? Latitude, double? Longitude)> GeocodeAsync(string city, string country)
    {
        var cacheKey = $"geo_{city}_{country}";

        if (_cache.TryGetValue(cacheKey, out (double?, double?) cachedCoords))
        {
            return cachedCoords;
        }

        await _rateLimiter.WaitAsync();
        try
        {
            await Task.Delay(REQUEST_DELAY_MS);

            var query = $"{city}, {country}";
            var url = $"{NOMINATIM_URL}?q={Uri.EscapeDataString(query)}&format=json&limit=1";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Geocoding failed for {city}, {country}: {response.StatusCode}");
                return (null, null);
            }

            var json = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<List<NominatimResult>>(json);

            if (results != null && results.Count > 0)
            {
                var result = results[0];
                var coords = (
                    Latitude: double.TryParse(result.lat, out var lat) ? lat : (double?)null,
                    Longitude: double.TryParse(result.lon, out var lon) ? lon : (double?)null
                );

                _cache.Set(cacheKey, coords, TimeSpan.FromDays(30));

                _logger.LogInformation($"Geocoded {city}, {country}: {coords.Latitude}, {coords.Longitude}");
                return coords;
            }

            _logger.LogWarning($"No geocoding results for {city}, {country}");
            return (null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error geocoding {city}, {country}");
            return (null, null);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    private class NominatimResult
    {
        public string lat { get; set; } = string.Empty;
        public string lon { get; set; } = string.Empty;
        public string display_name { get; set; } = string.Empty;
    }
}
