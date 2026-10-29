// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.GeoServices;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Interfaces.Routing;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Services.RouteOptimization;

namespace Klacks.Api.Infrastructure.Services.Routing;

/// <summary>
/// Server-side proxy for OpenRouteService directions with the profile of the requested transport mode
/// (driving-car, cycling-regular, foot-walking; mix routes as driving-car). Keeps the API key on the server: the key
/// is masked in every settings response, so a browser can never hold a usable one.
/// Answers null without any outbound call when the geo service policy switched OpenRouteService off.
/// </summary>
/// <param name="httpClient">Client used for the outbound call to the configured OpenRouteService instance</param>
/// <param name="geoServicesPolicy">Effective geo service configuration (on/off and OpenRouteService base URL)</param>
/// <param name="settingsRepository">Source of the stored, encrypted API key</param>
/// <param name="encryptionService">Decrypts the stored key for server-side use only</param>
public class OpenRouteServiceRoutingService : IRoutingService
{
    private const string SettingsType = Application.Constants.Settings.OPENROUTESERVICE_API_KEY;
    private const int MaxLoggedFailureLength = 300;

    private readonly HttpClient _httpClient;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ISettingsEncryptionService _encryptionService;
    private readonly ILogger<OpenRouteServiceRoutingService> _logger;
    private readonly IGeoServicesPolicy _geoServicesPolicy;

    public OpenRouteServiceRoutingService(
        HttpClient httpClient,
        ISettingsRepository settingsRepository,
        ISettingsEncryptionService encryptionService,
        IGeoServicesPolicy geoServicesPolicy,
        ILogger<OpenRouteServiceRoutingService> logger)
    {
        _geoServicesPolicy = geoServicesPolicy;
        _httpClient = httpClient;
        _settingsRepository = settingsRepository;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RoutePoint>?> GetRouteAsync(
        IReadOnlyList<RoutePoint> waypoints, ContainerTransportMode transportMode, CancellationToken cancellationToken)
    {
        if (!_geoServicesPolicy.OpenRouteService.IsEnabled)
        {
            return null;
        }

        var apiKey = await GetApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning(
                "OpenRouteService API key is not configured or could not be decrypted, the caller has to fall back to OSRM.");
            return null;
        }

        try
        {
            var payload = JsonSerializer.Serialize(new OrsDirectionsRequest
            {
                Coordinates = waypoints.Select(w => new[] { w.Lon, w.Lat }).ToList()
            });

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_geoServicesPolicy.OpenRouteService.BaseUrl}{RoutingProfile.OpenRouteServiceDirectionsGeoJsonPath(transportMode)}")
            {
                Content = new StringContent(payload, Encoding.UTF8, MediaTypeNames.Application.Json)
            };
            request.Headers.TryAddWithoutValidation("Authorization", apiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var failure = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogWarning(
                    "OpenRouteService returned {StatusCode} ({Reason}), the caller has to fall back to OSRM.",
                    (int)response.StatusCode,
                    Truncate(failure));
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var directions = JsonSerializer.Deserialize<OrsDirectionsResponse>(body);

            var coordinates = directions?.Features?.FirstOrDefault()?.Geometry?.Coordinates;
            if (coordinates == null || coordinates.Count == 0)
            {
                _logger.LogWarning("OpenRouteService returned no route geometry, the caller has to fall back to OSRM.");
                return null;
            }

            return coordinates
                .Where(c => c.Length >= 2)
                .Select(c => new RoutePoint(c[1], c[0]))
                .ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenRouteService request failed, the caller has to fall back to OSRM.");
            return null;
        }
    }

    private static string Truncate(string value) =>
        value.Length <= MaxLoggedFailureLength ? value : value[..MaxLoggedFailureLength];

    private async Task<string?> GetApiKeyAsync()
    {
        var setting = await _settingsRepository.GetSetting(SettingsType);
        if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
        {
            return null;
        }

        return _encryptionService.ProcessForReading(SettingsType, setting.Value);
    }

    private class OrsDirectionsRequest
    {
        [JsonPropertyName("coordinates")]
        public List<double[]> Coordinates { get; set; } = new();
    }

    private class OrsDirectionsResponse
    {
        [JsonPropertyName("features")]
        public List<OrsFeature>? Features { get; set; }
    }

    private class OrsFeature
    {
        [JsonPropertyName("geometry")]
        public OrsGeometry? Geometry { get; set; }
    }

    private class OrsGeometry
    {
        [JsonPropertyName("coordinates")]
        public List<double[]>? Coordinates { get; set; }
    }
}
