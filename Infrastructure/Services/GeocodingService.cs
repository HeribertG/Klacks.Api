using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Klacks.Api.Infrastructure.Services;

public interface IGeocodingService
{
    Task<(double? Latitude, double? Longitude)> GeocodeAsync(string city, string country);
    Task<(double? Latitude, double? Longitude)> GeocodeAddressAsync(string fullAddress, string country);
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

    public async Task<(double? Latitude, double? Longitude)> GeocodeAddressAsync(string fullAddress, string country)
    {
        var cacheKey = $"geo_addr_{fullAddress}_{country}";

        if (_cache.TryGetValue(cacheKey, out (double?, double?) cachedCoords))
        {
            return cachedCoords;
        }

        await _rateLimiter.WaitAsync();
        try
        {
            await Task.Delay(REQUEST_DELAY_MS);

            var query = $"{fullAddress}, {country}";
            var url = $"{NOMINATIM_URL}?q={Uri.EscapeDataString(query)}&format=json&limit=1&addressdetails=1";

            _logger.LogInformation("Geocoding full address: {Query}", query);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Geocoding failed for address {Address}: {StatusCode}", fullAddress, response.StatusCode);
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

                _logger.LogInformation("Geocoded address {Address}: {Lat}, {Lon} ({DisplayName})",
                    fullAddress, coords.Latitude, coords.Longitude, result.display_name);
                return coords;
            }

            _logger.LogWarning("No geocoding results for full address {Address}, trying fallback with ZIP and city", fullAddress);
            return await TryFallbackGeocoding(fullAddress, country, cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error geocoding address {Address}", fullAddress);
            return (null, null);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    private async Task<(double? Latitude, double? Longitude)> TryFallbackGeocoding(string fullAddress, string country, string cacheKey)
    {
        var addressMatch = System.Text.RegularExpressions.Regex.Match(fullAddress, @"^(.+?)\s*(\d+)?\s*,\s*(\d{4,5})\s+(.+)$");

        string? street = null;
        string? zip = null;
        string? city = null;

        if (addressMatch.Success)
        {
            street = addressMatch.Groups[1].Value.Trim();
            zip = addressMatch.Groups[3].Value;
            city = addressMatch.Groups[4].Value.Trim();
        }
        else
        {
            var zipCityMatch = System.Text.RegularExpressions.Regex.Match(fullAddress, @"(\d{4,5})\s+(\w+)");
            if (zipCityMatch.Success)
            {
                zip = zipCityMatch.Groups[1].Value;
                city = zipCityMatch.Groups[2].Value;
            }
        }

        if (string.IsNullOrEmpty(zip) || string.IsNullOrEmpty(city))
        {
            _logger.LogWarning("Could not extract ZIP and city from address {Address}", fullAddress);
            return (null, null);
        }

        var fallbackQueries = new List<string>();

        if (!string.IsNullOrEmpty(street))
        {
            fallbackQueries.Add($"{street}, {zip} {city}, {country}");
        }

        fallbackQueries.Add($"railway station, {zip} {city}, {country}");
        fallbackQueries.Add($"Bahnhof, {zip} {city}, {country}");
        fallbackQueries.Add($"gare, {zip} {city}, {country}");
        fallbackQueries.Add($"stazione, {zip} {city}, {country}");
        fallbackQueries.Add($"staziun, {zip} {city}, {country}");
        fallbackQueries.Add($"town hall, {zip} {city}, {country}");
        fallbackQueries.Add($"Rathaus, {zip} {city}, {country}");
        fallbackQueries.Add($"mairie, {zip} {city}, {country}");
        fallbackQueries.Add($"hôtel de ville, {zip} {city}, {country}");
        fallbackQueries.Add($"municipio, {zip} {city}, {country}");
        fallbackQueries.Add($"casa communala, {zip} {city}, {country}");
        fallbackQueries.Add($"{zip} {city}, {country}");
        fallbackQueries.Add($"{city}, {country}");

        foreach (var fallbackQuery in fallbackQueries)
        {
            _logger.LogInformation("Fallback geocoding attempt: {Query}", fallbackQuery);

            await Task.Delay(REQUEST_DELAY_MS);

            var url = $"{NOMINATIM_URL}?q={Uri.EscapeDataString(fallbackQuery)}&format=json&limit=1";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Fallback geocoding failed: {StatusCode}", response.StatusCode);
                continue;
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

                _logger.LogInformation("Fallback geocoded with query '{Query}': {Lat}, {Lon} ({DisplayName})",
                    fallbackQuery, coords.Latitude, coords.Longitude, result.display_name);
                return coords;
            }
        }

        _logger.LogWarning("All fallback geocoding attempts failed for address {Address}", fullAddress);
        return (null, null);
    }

    private class NominatimResult
    {
        public string lat { get; set; } = string.Empty;
        public string lon { get; set; } = string.Empty;
        public string display_name { get; set; } = string.Empty;
    }
}
