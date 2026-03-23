// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Service for address geocoding via Nominatim with rate limiting and memory cache.
/// @param httpClientFactory - Factory for the Nominatim HTTP client
/// @param cache - In-memory cache for geocoding results (30 days positive, 1 hour negative)
/// @param REQUEST_DELAY_MS - Minimum delay between Nominatim requests (500ms)
/// </summary>

using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Klacks.Api.Infrastructure.Services;

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

    public async Task<(double? Latitude, double? Longitude)> GeocodeAsync(string city, string country, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"geo_{city}_{country}";

        if (_cache.TryGetValue(cacheKey, out (double?, double?) cachedCoords))
        {
            return cachedCoords;
        }

        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(cacheKey, out cachedCoords))
            {
                return cachedCoords;
            }

            await Task.Delay(REQUEST_DELAY_MS, cancellationToken);

            var query = $"{city}, {country}";
            var url = $"{NOMINATIM_URL}?q={Uri.EscapeDataString(query)}&format=json&limit=1";

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Geocoding failed for {City}, {Country}: {StatusCode}", city, country, response.StatusCode);
                return (null, null);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var results = JsonSerializer.Deserialize<List<NominatimResult>>(json);

            if (results != null && results.Count > 0)
            {
                var coords = ExtractCoordinates(results[0]);
                _cache.Set(cacheKey, coords, TimeSpan.FromDays(30));
                _logger.LogInformation("Geocoded {City}, {Country}: {Latitude}, {Longitude}", city, country, coords.Latitude, coords.Longitude);
                return coords;
            }

            _logger.LogWarning("No geocoding results for {City}, {Country}", city, country);
            CacheNegativeResult(cacheKey);
            return (null, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error geocoding {City}, {Country}", city, country);
            return (null, null);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    public async Task<(double? Latitude, double? Longitude)> GeocodeAddressAsync(string fullAddress, string country, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"geo_addr_{fullAddress}_{country}";

        if (_cache.TryGetValue(cacheKey, out (double?, double?) cachedCoords))
        {
            return cachedCoords;
        }

        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(cacheKey, out cachedCoords))
            {
                return cachedCoords;
            }

            await Task.Delay(REQUEST_DELAY_MS, cancellationToken);

            var parsed = ParseAddressComponents(fullAddress);

            string url;
            if (parsed.HasValue)
            {
                var queryParams = new List<string>
                {
                    $"street={Uri.EscapeDataString(parsed.Value.Street)}",
                    $"city={Uri.EscapeDataString(parsed.Value.City)}",
                    $"postalcode={Uri.EscapeDataString(parsed.Value.Zip)}",
                    $"country={Uri.EscapeDataString(country)}",
                    "format=json",
                    "limit=1",
                    "addressdetails=1"
                };
                url = $"{NOMINATIM_URL}?{string.Join("&", queryParams)}";
                _logger.LogInformation("Geocoding structured: street={Street}, city={City}, zip={Zip}, country={Country}",
                    parsed.Value.Street, parsed.Value.City, parsed.Value.Zip, country);
            }
            else
            {
                var query = $"{fullAddress}, {country}";
                url = $"{NOMINATIM_URL}?q={Uri.EscapeDataString(query)}&format=json&limit=1&addressdetails=1";
                _logger.LogInformation("Geocoding free-text: {Query}", fullAddress);
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Geocoding failed for address {Address}: {StatusCode}", fullAddress, response.StatusCode);
                return (null, null);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var results = JsonSerializer.Deserialize<List<NominatimResult>>(json);

            if (results != null && results.Count > 0)
            {
                var coords = ExtractCoordinates(results[0]);
                _cache.Set(cacheKey, coords, TimeSpan.FromDays(30));
                _logger.LogInformation("Geocoded address {Address}: {Lat}, {Lon} ({DisplayName})",
                    fullAddress, coords.Latitude, coords.Longitude, results[0].display_name);
                return coords;
            }

            _logger.LogWarning("No geocoding results for address {Address}, trying fallback", fullAddress);
            return await TryFallbackGeocoding(fullAddress, country, cacheKey, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
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

    private static (string Street, string Zip, string City)? ParseAddressComponents(string fullAddress)
    {
        var match = Regex.Match(fullAddress, @"^(.+?)\s*,\s*(\d{4,5})\s+(.+)$");
        if (match.Success)
        {
            return (match.Groups[1].Value.Trim(), match.Groups[2].Value, match.Groups[3].Value.Trim());
        }

        return null;
    }

    private async Task<(double? Latitude, double? Longitude)> TryFallbackGeocoding(
        string fullAddress, string country, string cacheKey, CancellationToken cancellationToken)
    {
        var parsed = ParseAddressComponents(fullAddress);
        string? street = parsed?.Street;
        string? zip = parsed?.Zip;
        string? city = parsed?.City;

        if (string.IsNullOrEmpty(zip) || string.IsNullOrEmpty(city))
        {
            var zipCityMatch = Regex.Match(fullAddress, @"(\d{4,5})\s+([\p{L}\p{M}\s\.\-'']+)");
            if (zipCityMatch.Success)
            {
                zip = zipCityMatch.Groups[1].Value;
                city = zipCityMatch.Groups[2].Value.Trim();
            }
        }

        if (string.IsNullOrEmpty(zip) || string.IsNullOrEmpty(city))
        {
            _logger.LogWarning("Could not extract ZIP and city from address {Address}", fullAddress);
            CacheNegativeResult(cacheKey);
            return (null, null);
        }

        var fallbackSteps = new List<(string Type, Func<string> BuildUrl)>();

        if (!string.IsNullOrEmpty(street))
        {
            fallbackSteps.Add(("structured-street", () =>
            {
                var p = new List<string>
                {
                    $"street={Uri.EscapeDataString(street)}",
                    $"city={Uri.EscapeDataString(city)}",
                    $"country={Uri.EscapeDataString(country)}",
                    "format=json", "limit=1"
                };
                return $"{NOMINATIM_URL}?{string.Join("&", p)}";
            }));
        }

        fallbackSteps.Add(("structured-zip", () =>
            $"{NOMINATIM_URL}?postalcode={Uri.EscapeDataString(zip)}&city={Uri.EscapeDataString(city)}&country={Uri.EscapeDataString(country)}&format=json&limit=1"));

        fallbackSteps.Add(("free-text-city", () =>
            $"{NOMINATIM_URL}?q={Uri.EscapeDataString($"{city}, {country}")}&format=json&limit=1"));

        foreach (var (type, buildUrl) in fallbackSteps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Delay(REQUEST_DELAY_MS, cancellationToken);

            var url = buildUrl();
            _logger.LogInformation("Fallback geocoding ({Type}): {Url}", type, url);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) continue;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var results = JsonSerializer.Deserialize<List<NominatimResult>>(json);

            if (results != null && results.Count > 0)
            {
                var coords = ExtractCoordinates(results[0]);
                _cache.Set(cacheKey, coords, TimeSpan.FromDays(30));
                _logger.LogInformation("Fallback geocoded ({Type}): {Lat}, {Lon} ({DisplayName})",
                    type, coords.Latitude, coords.Longitude, results[0].display_name);
                return coords;
            }
        }

        _logger.LogWarning("All fallback geocoding attempts failed for address {Address}", fullAddress);
        CacheNegativeResult(cacheKey);
        return (null, null);
    }

    public async Task<GeocodingValidationResult> ValidateExactAddressAsync(string? street, string postalCode, string city, string country)
    {
        var cacheKey = $"geo_validate_{street}_{postalCode}_{city}_{country}";

        if (_cache.TryGetValue(cacheKey, out GeocodingValidationResult? cachedResult) && cachedResult != null)
        {
            return cachedResult;
        }

        await _rateLimiter.WaitAsync();
        try
        {
            if (_cache.TryGetValue(cacheKey, out cachedResult) && cachedResult != null)
            {
                return cachedResult;
            }

            await Task.Delay(REQUEST_DELAY_MS);

            var queryParams = new List<string>
            {
                $"postalcode={Uri.EscapeDataString(postalCode)}",
                $"city={Uri.EscapeDataString(city)}",
                $"country={Uri.EscapeDataString(country)}",
                "format=json",
                "limit=1",
                "addressdetails=1"
            };

            if (!string.IsNullOrEmpty(street))
            {
                queryParams.Insert(0, $"street={Uri.EscapeDataString(street)}");
            }

            var url = $"{NOMINATIM_URL}?{string.Join("&", queryParams)}";
            _logger.LogInformation("Validating exact address: street={Street}, postalCode={PostalCode}, city={City}, country={Country}", street, postalCode, city, country);

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return new GeocodingValidationResult { Found = false, MatchType = "error" };
            }

            var json = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<List<NominatimResult>>(json);

            if (results == null || results.Count == 0)
            {
                _logger.LogWarning("Address not found: {Street}, {PostalCode} {City}, {Country}", street, postalCode, city, country);

                if (!string.IsNullOrEmpty(street))
                {
                    var fallbackResult = await ValidateWithoutStreetAsync(postalCode, city, country);
                    if (fallbackResult.Found)
                    {
                        fallbackResult.ExactMatch = false;
                        fallbackResult.MatchType = "city_only";
                        _cache.Set(cacheKey, fallbackResult, TimeSpan.FromDays(1));
                        return fallbackResult;
                    }
                }

                var notFound = new GeocodingValidationResult { Found = false, MatchType = "not_found" };
                _cache.Set(cacheKey, notFound, TimeSpan.FromHours(1));
                return notFound;
            }

            var firstResult = results[0];
            var (lat, lon) = ExtractCoordinates(firstResult);

            var isExact = !string.IsNullOrEmpty(street) &&
                         firstResult.display_name.Contains(postalCode) &&
                         ContainsStreetName(firstResult.display_name, street);

            var result2 = new GeocodingValidationResult
            {
                Found = true,
                ExactMatch = isExact,
                Latitude = lat,
                Longitude = lon,
                ReturnedAddress = firstResult.display_name,
                MatchType = isExact ? "exact" : "approximate",
                State = firstResult.address?.state
            };

            _cache.Set(cacheKey, result2, TimeSpan.FromDays(7));
            _logger.LogInformation("Address validation result: Found={Found}, ExactMatch={ExactMatch}, ReturnedAddress={ReturnedAddress}",
                result2.Found, result2.ExactMatch, result2.ReturnedAddress);

            return result2;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating address");
            return new GeocodingValidationResult { Found = false, MatchType = "error" };
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    private async Task<GeocodingValidationResult> ValidateWithoutStreetAsync(string postalCode, string city, string country)
    {
        await Task.Delay(REQUEST_DELAY_MS);

        var url = $"{NOMINATIM_URL}?postalcode={Uri.EscapeDataString(postalCode)}&city={Uri.EscapeDataString(city)}&country={Uri.EscapeDataString(country)}&format=json&limit=1&addressdetails=1";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            return new GeocodingValidationResult { Found = false };
        }

        var json = await response.Content.ReadAsStringAsync();
        var results = JsonSerializer.Deserialize<List<NominatimResult>>(json);

        if (results == null || results.Count == 0)
        {
            return new GeocodingValidationResult { Found = false };
        }

        var first = results[0];
        var (validLat, validLon) = ExtractCoordinates(first);
        return new GeocodingValidationResult
        {
            Found = true,
            Latitude = validLat,
            Longitude = validLon,
            ReturnedAddress = first.display_name,
            State = first.address?.state
        };
    }

    public async Task<List<AddressSuggestion>> GetAddressSuggestionsAsync(
        string? street, string postalCode, string city, string country, int limit = 5)
    {
        var cacheKey = $"geo_suggest_{street}_{postalCode}_{city}_{country}_{limit}";

        if (_cache.TryGetValue(cacheKey, out List<AddressSuggestion>? cached) && cached != null)
        {
            return cached;
        }

        await _rateLimiter.WaitAsync();
        try
        {
            if (_cache.TryGetValue(cacheKey, out cached) && cached != null)
            {
                return cached;
            }

            await Task.Delay(REQUEST_DELAY_MS);

            var query = BuildSuggestionQuery(street, postalCode, city, country);
            var url = $"{NOMINATIM_URL}?q={Uri.EscapeDataString(query)}&format=json&limit={limit}&addressdetails=1";

            _logger.LogInformation("Fetching address suggestions: {Query}", query);

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Suggestion request failed: {StatusCode}", response.StatusCode);
                return [];
            }

            var json = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<List<NominatimResult>>(json);

            if (results == null || results.Count == 0)
            {
                var fallbackSuggestions = await TryFallbackSuggestions(postalCode, city, country, limit);
                _cache.Set(cacheKey, fallbackSuggestions, TimeSpan.FromHours(1));
                return fallbackSuggestions;
            }

            var suggestions = results
                .Select(r => (Result: r, Coords: ExtractCoordinates(r)))
                .Where(x => x.Coords.Latitude.HasValue && x.Coords.Longitude.HasValue)
                .Select(x => new AddressSuggestion
                {
                    Latitude = x.Coords.Latitude!.Value,
                    Longitude = x.Coords.Longitude!.Value,
                    DisplayName = x.Result.display_name
                })
                .ToList();

            _cache.Set(cacheKey, suggestions, TimeSpan.FromDays(7));
            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching address suggestions");
            return [];
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    private static string BuildSuggestionQuery(string? street, string postalCode, string city, string country)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(street)) parts.Add(street);
        if (!string.IsNullOrWhiteSpace(postalCode)) parts.Add(postalCode);
        if (!string.IsNullOrWhiteSpace(city)) parts.Add(city);
        if (!string.IsNullOrWhiteSpace(country)) parts.Add(country);
        return string.Join(", ", parts);
    }

    private async Task<List<AddressSuggestion>> TryFallbackSuggestions(
        string postalCode, string city, string country, int limit)
    {
        await Task.Delay(REQUEST_DELAY_MS);

        var query = $"{postalCode} {city}, {country}";
        var url = $"{NOMINATIM_URL}?q={Uri.EscapeDataString(query)}&format=json&limit={limit}";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode) return [];

        var json = await response.Content.ReadAsStringAsync();
        var results = JsonSerializer.Deserialize<List<NominatimResult>>(json);

        if (results == null || results.Count == 0) return [];

        return results
            .Select(r => (Result: r, Coords: ExtractCoordinates(r)))
            .Where(x => x.Coords.Latitude.HasValue && x.Coords.Longitude.HasValue)
            .Select(x => new AddressSuggestion
            {
                Latitude = x.Coords.Latitude!.Value,
                Longitude = x.Coords.Longitude!.Value,
                DisplayName = x.Result.display_name
            })
            .ToList();
    }

    private void CacheNegativeResult(string cacheKey)
    {
        _cache.Set(cacheKey, ((double?)null, (double?)null), TimeSpan.FromHours(1));
    }

    private static bool ContainsStreetName(string displayName, string street)
    {
        var streetName = street.Split(' ')[0].ToLowerInvariant();
        return displayName.ToLowerInvariant().Contains(streetName);
    }

    private static double? ParseCoordinate(string value)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    private static (double? Latitude, double? Longitude) ExtractCoordinates(NominatimResult result)
    {
        return (ParseCoordinate(result.lat), ParseCoordinate(result.lon));
    }

    private class NominatimResult
    {
        public string lat { get; set; } = string.Empty;
        public string lon { get; set; } = string.Empty;
        public string display_name { get; set; } = string.Empty;
        public NominatimAddress? address { get; set; }
    }

    private class NominatimAddress
    {
        public string? state { get; set; }
        public string? county { get; set; }
        public string? postcode { get; set; }
        public string? city { get; set; }
        public string? town { get; set; }
        public string? village { get; set; }
        public string? road { get; set; }
        public string? house_number { get; set; }
        public string? country { get; set; }
        public string? country_code { get; set; }
    }
}
