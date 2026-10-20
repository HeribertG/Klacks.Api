// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Service for address geocoding via Nominatim with rate limiting and memory cache.
/// Does nothing (no HTTP, no cache) when the geo service policy switched geocoding off.
/// @param httpClientFactory - Factory for the Nominatim HTTP client
/// @param geoServicesPolicy - Effective geo service configuration (on/off and Nominatim base URL)
/// @param cache - In-memory cache for geocoding results (30 days positive, 1 hour negative)
/// @param REQUEST_DELAY_MS - Minimum delay between Nominatim requests (1100ms)
/// @param SearchSlotWaitTimeout - Longest time the interactive search waits for the shared Nominatim slot before
/// answering Unavailable, so searches never pile up in front of address validation on save
/// @param SearchRequestTimeout - Longest time the interactive search holds the slot for its Nominatim request
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.GeoServices;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Logging;
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
    private readonly string _searchUrl;
    private const int REQUEST_DELAY_MS = 1100;
    private const string MATCH_TYPE_ERROR = "error";
    private const string MATCH_TYPE_DISABLED = "geocoding_disabled";

    public static readonly TimeSpan SearchSlotWaitTimeout = TimeSpan.FromSeconds(3);
    public static readonly TimeSpan SearchRequestTimeout = TimeSpan.FromSeconds(10);

    public GeocodingService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IGeoServicesPolicy geoServicesPolicy,
        ILogger<GeocodingService> logger)
    {
        IsEnabled = geoServicesPolicy.Geocoding.IsEnabled;
        _searchUrl = $"{geoServicesPolicy.Geocoding.BaseUrl}{GeoServicePaths.NominatimSearch}";
        _httpClient = httpClientFactory.CreateClient(ExternalHttpClientConstants.NominatimClientName);
        _cache = cache;
        _logger = logger;
        _rateLimiter = new SemaphoreSlim(1, 1);
    }

    public bool IsEnabled { get; }

    public async Task<(double? Latitude, double? Longitude)> GeocodeAsync(string city, string country, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return (null, null);
        }

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
            var url = $"{_searchUrl}?q={Uri.EscapeDataString(query)}&format=json&limit=1";

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
                _cache.Set(cacheKey, coords, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromDays(30)).SetSize(1));
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
        if (!IsEnabled)
        {
            return (null, null);
        }

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
                url = $"{_searchUrl}?{string.Join("&", queryParams)}";
                _logger.LogInformation("Geocoding structured: street={Street}, city={City}, zip={Zip}, country={Country}",
                    parsed.Value.Street, parsed.Value.City, parsed.Value.Zip, country);
            }
            else
            {
                var query = $"{fullAddress}, {country}";
                url = $"{_searchUrl}?q={Uri.EscapeDataString(query)}&format=json&limit=1&addressdetails=1";
                _logger.LogInformation("Geocoding free-text: {Query}", fullAddress.ForLog());
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Geocoding failed for address {Address}: {StatusCode}", fullAddress.ForLog(), response.StatusCode);
                return (null, null);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var results = JsonSerializer.Deserialize<List<NominatimResult>>(json);

            if (results != null && results.Count > 0)
            {
                var coords = ExtractCoordinates(results[0]);
                _cache.Set(cacheKey, coords, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromDays(30)).SetSize(1));
                _logger.LogInformation("Geocoded address {Address}: {Lat}, {Lon} ({DisplayName})",
                    fullAddress.ForLog(), coords.Latitude, coords.Longitude, results[0].display_name.ForLog());
                return coords;
            }

            _logger.LogWarning("No geocoding results for address {Address}, trying fallback", fullAddress.ForLog());
            return await TryFallbackGeocoding(fullAddress, country, cacheKey, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error geocoding address {Address}", fullAddress.ForLog());
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
            _logger.LogWarning("Could not extract ZIP and city from address {Address}", fullAddress.ForLog());
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
                return $"{_searchUrl}?{string.Join("&", p)}";
            }));
        }

        fallbackSteps.Add(("structured-zip", () =>
            $"{_searchUrl}?postalcode={Uri.EscapeDataString(zip)}&city={Uri.EscapeDataString(city)}&country={Uri.EscapeDataString(country)}&format=json&limit=1"));

        fallbackSteps.Add(("free-text-city", () =>
            $"{_searchUrl}?q={Uri.EscapeDataString($"{city}, {country}")}&format=json&limit=1"));

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
                _cache.Set(cacheKey, coords, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromDays(30)).SetSize(1));
                _logger.LogInformation("Fallback geocoded ({Type}): {Lat}, {Lon} ({DisplayName})",
                    type, coords.Latitude, coords.Longitude, results[0].display_name);
                return coords;
            }
        }

        _logger.LogWarning("All fallback geocoding attempts failed for address {Address}", fullAddress.ForLog());
        CacheNegativeResult(cacheKey);
        return (null, null);
    }

    public async Task<GeocodingValidationResult> ValidateExactAddressAsync(string? street, string postalCode, string city, string country)
    {
        if (!IsEnabled)
        {
            return new GeocodingValidationResult { Found = false, Disabled = true, MatchType = MATCH_TYPE_DISABLED };
        }

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

            var url = $"{_searchUrl}?{string.Join("&", queryParams)}";
            _logger.LogInformation("Validating exact address: street={Street}, postalCode={PostalCode}, city={City}, country={Country}", street.ForLog(), postalCode.ForLog(), city.ForLog(), country.ForLog());

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Nominatim answered {StatusCode} while validating an address", (int)response.StatusCode);
                return UnavailableResult();
            }

            var json = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<List<NominatimResult>>(json);

            if (results == null || results.Count == 0)
            {
                _logger.LogWarning("Address not found: {Street}, {PostalCode} {City}, {Country}", street.ForLog(), postalCode.ForLog(), city.ForLog(), country.ForLog());

                if (!string.IsNullOrEmpty(street))
                {
                    var fallbackResult = await ValidateWithoutStreetAsync(postalCode, city, country);
                    if (fallbackResult.ServiceUnavailable)
                    {
                        return fallbackResult;
                    }

                    if (fallbackResult.Found)
                    {
                        fallbackResult.ExactMatch = false;
                        fallbackResult.MatchType = "city_only";
                        _cache.Set(cacheKey, fallbackResult, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromDays(1)).SetSize(1));
                        return fallbackResult;
                    }
                }

                var notFound = new GeocodingValidationResult { Found = false, MatchType = "not_found" };
                _cache.Set(cacheKey, notFound, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(1)).SetSize(1));
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
                State = firstResult.address?.state,
                County = firstResult.address?.county
            };

            _cache.Set(cacheKey, result2, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromDays(7)).SetSize(1));
            _logger.LogInformation("Address validation result: Found={Found}, ExactMatch={ExactMatch}, ReturnedAddress={ReturnedAddress}",
                result2.Found, result2.ExactMatch, result2.ReturnedAddress);

            return result2;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating address");
            return UnavailableResult();
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    // Never cached: an outage must not make a real address look non-existent after Nominatim recovers.
    private static GeocodingValidationResult UnavailableResult() =>
        new() { Found = false, ServiceUnavailable = true, MatchType = MATCH_TYPE_ERROR };

    private async Task<GeocodingValidationResult> ValidateWithoutStreetAsync(string postalCode, string city, string country)
    {
        await Task.Delay(REQUEST_DELAY_MS);

        var url = $"{_searchUrl}?postalcode={Uri.EscapeDataString(postalCode)}&city={Uri.EscapeDataString(city)}&country={Uri.EscapeDataString(country)}&format=json&limit=1&addressdetails=1";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Nominatim answered {StatusCode} on the city-level address fallback", (int)response.StatusCode);
            return UnavailableResult();
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
            State = first.address?.state,
            County = first.address?.county
        };
    }

    public async Task<List<AddressSuggestion>> GetAddressSuggestionsAsync(
        string? street, string postalCode, string city, string country, int limit = 5)
    {
        if (!IsEnabled)
        {
            return [];
        }

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
            var url = $"{_searchUrl}?q={Uri.EscapeDataString(query)}&format=json&limit={limit}&addressdetails=1";

            _logger.LogInformation("Fetching address suggestions: {Query}", query.ForLog());

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
                _cache.Set(cacheKey, fallbackSuggestions, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(1)).SetSize(1));
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

            _cache.Set(cacheKey, suggestions, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromDays(7)).SetSize(1));
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

    public async Task<GeocodingSearchResult> SearchAsync(string query, string? language, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return new GeocodingSearchResult(GeocodingSearchOutcome.Disabled, null, null, null);
        }

        var cacheKey = $"geo_search_{query}_{language}";
        if (_cache.TryGetValue(cacheKey, out GeocodingSearchResult? cached) && cached != null)
        {
            return cached;
        }

        if (!await _rateLimiter.WaitAsync(SearchSlotWaitTimeout, cancellationToken))
        {
            _logger.LogWarning("Geocoding search gave up waiting for the shared Nominatim slot");
            return new GeocodingSearchResult(GeocodingSearchOutcome.Unavailable, null, null, null);
        }

        try
        {
            if (_cache.TryGetValue(cacheKey, out cached) && cached != null)
            {
                return cached;
            }

            await Task.Delay(REQUEST_DELAY_MS, cancellationToken);

            var url = $"{_searchUrl}?format=json&q={Uri.EscapeDataString(query)}&limit=1";
            if (!string.IsNullOrWhiteSpace(language))
            {
                url += $"&accept-language={Uri.EscapeDataString(language)}";
            }

            using var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            requestTimeout.CancelAfter(SearchRequestTimeout);

            using var response = await _httpClient.GetAsync(url, requestTimeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Geocoding search failed: {StatusCode}", (int)response.StatusCode);
                return new GeocodingSearchResult(GeocodingSearchOutcome.Unavailable, null, null, null);
            }

            var json = await response.Content.ReadAsStringAsync(requestTimeout.Token);
            var results = JsonSerializer.Deserialize<List<NominatimResult>>(json);
            var first = results?.FirstOrDefault();
            var (latitude, longitude) = first != null ? ExtractCoordinates(first) : (null, null);

            if (first == null || !latitude.HasValue || !longitude.HasValue)
            {
                var notFound = new GeocodingSearchResult(GeocodingSearchOutcome.NotFound, null, null, null);
                _cache.Set(cacheKey, notFound, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(1)).SetSize(1));
                return notFound;
            }

            var found = new GeocodingSearchResult(GeocodingSearchOutcome.Found, latitude, longitude, first.display_name);
            _cache.Set(cacheKey, found, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromDays(30)).SetSize(1));
            return found;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Geocoding search failed");
            return new GeocodingSearchResult(GeocodingSearchOutcome.Unavailable, null, null, null);
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
        var url = $"{_searchUrl}?q={Uri.EscapeDataString(query)}&format=json&limit={limit}";

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
        _cache.Set(cacheKey, ((double?)null, (double?)null), new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(1)).SetSize(1));
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
