// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds distance and duration matrices using OSRM, OpenRouteService or Haversine fallback.
/// </summary>
/// <param name="_settingsReader">Repository for reading application settings (e.g. API keys)</param>
/// <param name="_encryptionService">Decrypts encrypted setting values</param>
/// <param name="_cache">In-memory cache for distance/duration matrices</param>
/// <param name="_httpClient">HTTP client for external routing API calls</param>

using System.Text;
using System.Text.Json;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Interfaces.Staffs;
using Microsoft.Extensions.Caching.Memory;

namespace Klacks.Api.Domain.Services.RouteOptimization;

public class DistanceMatrixBuilder : IDistanceMatrixBuilder
{
    private readonly ISettingsReader _settingsReader;
    private readonly ISettingsEncryptionService _encryptionService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DistanceMatrixBuilder> _logger;
    private readonly HttpClient _httpClient;
    private const string OSRM_BASE_URL = "https://router.project-osrm.org";
    private const string OPENROUTESERVICE_BASE_URL = "https://api.openrouteservice.org/v2";
    private const string OPENROUTESERVICE_API_KEY_SETTING = Application.Constants.Settings.OPENROUTESERVICE_API_KEY;

    public DistanceMatrixBuilder(
        ISettingsReader settingsReader,
        ISettingsEncryptionService encryptionService,
        IMemoryCache cache,
        ILogger<DistanceMatrixBuilder> logger,
        IHttpClientFactory httpClientFactory)
    {
        _settingsReader = settingsReader;
        _encryptionService = encryptionService;
        _cache = cache;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<(double[,] distanceMatrix, double[,] durationMatrix, Dictionary<string, double[,]>? durationMatricesByProfile)> BuildDistanceMatrixAsync(
        List<Location> locations,
        ContainerTransportMode transportMode)
    {
        _logger.LogInformation("=== BuildDistanceMatrixAsync START ===");
        _logger.LogInformation("TransportMode received: {TransportMode} (int value: {IntValue})", transportMode, (int)transportMode);

        var apiKey = await GetOpenRouteServiceApiKeyAsync();
        var isOpenRouteServiceConfigured = !string.IsNullOrEmpty(apiKey);
        _logger.LogInformation("OpenRouteService configured: {IsConfigured}", isOpenRouteServiceConfigured);

        var size = locations.Count;
        var distanceMatrix = new double[size, size];
        var durationMatrix = new double[size, size];

        if (transportMode == ContainerTransportMode.Mix)
        {
            _logger.LogInformation("TransportMode is Mix - building mixed matrices for all profiles");
            return await BuildMixedDistanceMatrixAsync(locations);
        }

        var servicePrefix = isOpenRouteServiceConfigured ? "ors" : "osrm";
        var cacheKey = $"{servicePrefix}_matrix_{transportMode}_{string.Join("_", locations.Select(l => $"{l.Latitude:F6}_{l.Longitude:F6}"))}";
        _logger.LogInformation("TransportMode: {TransportMode}, Cache key: {CacheKey}", transportMode, cacheKey);

        if (_cache.TryGetValue(cacheKey, out (double[,] dist, double[,] dur)? cached) && cached != null)
        {
            _logger.LogInformation("CACHE HIT - Using cached distance/duration matrix for TransportMode '{TransportMode}'", transportMode);
            return (cached.Value.dist, cached.Value.dur, null);
        }

        _logger.LogInformation("CACHE MISS - Fetching new data for TransportMode '{TransportMode}'", transportMode);

        try
        {
            (double[,] distanceMatrix, double[,] durationMatrix) result;

            if (isOpenRouteServiceConfigured)
            {
                _logger.LogInformation("Using OpenRouteService API");
                result = await GetOpenRouteServiceMatrixAsync(locations, transportMode, apiKey!);
            }
            else
            {
                _logger.LogInformation("Using OSRM (public server - only driving profile supported for accurate times)");
                result = await GetOsrmDistanceMatrixAsync(locations, transportMode);
            }

            _cache.Set(cacheKey, result, TimeSpan.FromDays(7));
            _logger.LogInformation("Successfully retrieved distance/duration matrix");
            return (result.distanceMatrix, result.durationMatrix, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get routing matrix, falling back to Haversine distances with estimated times");
            distanceMatrix = BuildHaversineDistanceMatrix(locations);
            durationMatrix = BuildEstimatedDurationMatrix(distanceMatrix, transportMode);
            return (distanceMatrix, durationMatrix, null);
        }
    }

    public async Task<(double[,] distanceMatrix, double[,] durationMatrix, Dictionary<string, double[,]> durationMatricesByProfile)> BuildMixedDistanceMatrixAsync(
        List<Location> locations)
    {
        var size = locations.Count;
        var profiles = new[] { "driving", "cycling", "foot" };
        var durationMatricesByProfile = new Dictionary<string, double[,]>();
        double[,] distanceMatrix = new double[size, size];
        double[,] defaultDurationMatrix = new double[size, size];

        _logger.LogInformation("Building mixed transport matrices - fetching all profiles (driving, cycling, foot)");

        foreach (var profile in profiles)
        {
            var cacheKey = $"osrm_matrix_{profile}_{string.Join("_", locations.Select(l => $"{l.Latitude:F6}_{l.Longitude:F6}"))}";

            if (_cache.TryGetValue(cacheKey, out (double[,] dist, double[,] dur)? cached) && cached != null)
            {
                _logger.LogInformation("Using cached OSRM matrix for profile '{Profile}'", profile);
                durationMatricesByProfile[profile] = cached.Value.dur;
                if (profile == "driving")
                {
                    distanceMatrix = cached.Value.dist;
                    defaultDurationMatrix = cached.Value.dur;
                }
                continue;
            }

            try
            {
                var transportMode = profile switch
                {
                    "driving" => ContainerTransportMode.ByCar,
                    "cycling" => ContainerTransportMode.ByBicycle,
                    "foot" => ContainerTransportMode.ByFoot,
                    _ => ContainerTransportMode.ByCar
                };

                var result = await GetOsrmDistanceMatrixAsync(locations, transportMode);
                _cache.Set(cacheKey, result, TimeSpan.FromDays(7));

                durationMatricesByProfile[profile] = result.durationMatrix;

                if (profile == "driving")
                {
                    distanceMatrix = result.distanceMatrix;
                    defaultDurationMatrix = result.durationMatrix;
                }

                _logger.LogInformation("Retrieved OSRM matrix for profile '{Profile}'", profile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get OSRM matrix for profile '{Profile}', using fallback", profile);

                var haversineMatrix = BuildHaversineDistanceMatrix(locations);
                var transportMode = profile switch
                {
                    "driving" => ContainerTransportMode.ByCar,
                    "cycling" => ContainerTransportMode.ByBicycle,
                    "foot" => ContainerTransportMode.ByFoot,
                    _ => ContainerTransportMode.ByCar
                };
                var fallbackDuration = BuildEstimatedDurationMatrix(haversineMatrix, transportMode);
                durationMatricesByProfile[profile] = fallbackDuration;

                if (profile == "driving")
                {
                    distanceMatrix = haversineMatrix;
                    defaultDurationMatrix = fallbackDuration;
                }
            }
        }

        return (distanceMatrix, defaultDurationMatrix, durationMatricesByProfile);
    }

    public double[,] BuildEstimatedDurationMatrix(double[,] distanceMatrix, ContainerTransportMode transportMode)
    {
        var size = distanceMatrix.GetLength(0);
        var durationMatrix = new double[size, size];

        double speedKmh = transportMode switch
        {
            ContainerTransportMode.ByCar => 50.0,
            ContainerTransportMode.ByBicycle => 15.0,
            ContainerTransportMode.ByFoot => 5.0,
            ContainerTransportMode.Mix => 30.0,
            _ => 50.0
        };

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                var distanceKm = distanceMatrix[i, j];
                var hours = distanceKm / speedKmh;
                durationMatrix[i, j] = hours * 3600;
            }
        }

        return durationMatrix;
    }

    public double[,] BuildHaversineDistanceMatrix(List<Location> locations)
    {
        var size = locations.Count;
        var matrix = new double[size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (i == j)
                {
                    matrix[i, j] = 0.0;
                }
                else
                {
                    matrix[i, j] = TravelTimeCalculator.CalculateHaversineDistance(
                        locations[i].Latitude, locations[i].Longitude,
                        locations[j].Latitude, locations[j].Longitude);
                }
            }
        }

        return matrix;
    }

    private async Task<string?> GetOpenRouteServiceApiKeyAsync()
    {
        var setting = await _settingsReader.GetSetting(OPENROUTESERVICE_API_KEY_SETTING);
        if (setting == null || string.IsNullOrEmpty(setting.Value))
        {
            return null;
        }

        return _encryptionService.ProcessForReading(setting.Type, setting.Value);
    }

    private string GetOpenRouteServiceProfile(ContainerTransportMode transportMode)
    {
        return transportMode switch
        {
            ContainerTransportMode.ByCar => "driving-car",
            ContainerTransportMode.ByBicycle => "cycling-regular",
            ContainerTransportMode.ByFoot => "foot-walking",
            ContainerTransportMode.Mix => "driving-car",
            _ => "driving-car"
        };
    }

    private string GetOsrmProfile(ContainerTransportMode transportMode)
    {
        return transportMode switch
        {
            ContainerTransportMode.ByCar => "driving",
            ContainerTransportMode.ByBicycle => "cycling",
            ContainerTransportMode.ByFoot => "foot",
            ContainerTransportMode.Mix => "driving",
            _ => "driving"
        };
    }

    private double GetSpeedForTransportMode(ContainerTransportMode transportMode)
    {
        return transportMode switch
        {
            ContainerTransportMode.ByCar => 40.0,
            ContainerTransportMode.ByBicycle => 15.0,
            ContainerTransportMode.ByFoot => 5.0,
            ContainerTransportMode.Mix => 30.0,
            _ => 40.0
        };
    }

    private async Task<(double[,] distanceMatrix, double[,] durationMatrix)> GetOsrmDistanceMatrixAsync(
        List<Location> locations,
        ContainerTransportMode transportMode)
    {
        var size = locations.Count;
        var distanceMatrix = new double[size, size];
        var durationMatrix = new double[size, size];

        var coordinates = string.Join(";", locations.Select(l => $"{l.Longitude:F6},{l.Latitude:F6}"));
        var url = $"{OSRM_BASE_URL}/table/v1/driving/{coordinates}?annotations=distance,duration";

        _logger.LogInformation("Requesting OSRM table API for distances (driving profile): {Url}", url);

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.GetProperty("code").GetString() != "Ok")
        {
            throw new Exception($"OSRM API returned error: {root.GetProperty("code").GetString()}");
        }

        var distances = root.GetProperty("distances");
        var durations = root.GetProperty("durations");

        double speedKmh = GetSpeedForTransportMode(transportMode);
        _logger.LogInformation("Using speed {SpeedKmh} km/h for TransportMode {TransportMode}", speedKmh, transportMode);

        for (int i = 0; i < size; i++)
        {
            var distanceRow = distances[i];
            var durationRow = durations[i];
            for (int j = 0; j < size; j++)
            {
                var distanceMeters = distanceRow[j].GetDouble();
                var distanceKm = distanceMeters / 1000.0;
                distanceMatrix[i, j] = distanceKm;

                if (transportMode == ContainerTransportMode.ByCar)
                {
                    var durationSeconds = durationRow[j].GetDouble();
                    durationMatrix[i, j] = durationSeconds;
                }
                else
                {
                    var hours = distanceKm / speedKmh;
                    durationMatrix[i, j] = hours * 3600;
                }
            }
        }

        if (transportMode == ContainerTransportMode.ByCar)
        {
            _logger.LogInformation("Using OSRM driving times directly");
        }
        else
        {
            _logger.LogInformation(
                "Calculated travel times for {TransportMode} based on {SpeedKmh} km/h (OSRM public server only supports driving)",
                transportMode, speedKmh);
        }

        return (distanceMatrix, durationMatrix);
    }

    private async Task<(double[,] distanceMatrix, double[,] durationMatrix)> GetOpenRouteServiceMatrixAsync(
        List<Location> locations,
        ContainerTransportMode transportMode,
        string apiKey)
    {
        var size = locations.Count;
        var distanceMatrix = new double[size, size];
        var durationMatrix = new double[size, size];

        var profile = GetOpenRouteServiceProfile(transportMode);
        var url = $"{OPENROUTESERVICE_BASE_URL}/matrix/{profile}";

        _logger.LogInformation("Requesting OpenRouteService matrix API with profile '{Profile}': {Url}", profile, url);

        var locationsArray = locations.Select(l => new[] { l.Longitude, l.Latitude }).ToArray();
        var requestBody = new
        {
            locations = locationsArray,
            metrics = new[] { "distance", "duration" },
            units = "m"
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = httpContent;
        request.Headers.Add("Authorization", apiKey);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("OpenRouteService API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
            throw new Exception($"OpenRouteService API returned error: {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var distances = root.GetProperty("distances");
        var durations = root.GetProperty("durations");

        for (int i = 0; i < size; i++)
        {
            var distanceRow = distances[i];
            var durationRow = durations[i];
            for (int j = 0; j < size; j++)
            {
                var distanceMeters = distanceRow[j].GetDouble();
                distanceMatrix[i, j] = distanceMeters / 1000.0;

                var durationSeconds = durationRow[j].GetDouble();
                durationMatrix[i, j] = durationSeconds;
            }
        }

        _logger.LogInformation(
            "OpenRouteService returned real travel times for profile '{Profile}'",
            profile);

        return (distanceMatrix, durationMatrix);
    }
}
