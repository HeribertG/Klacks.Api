// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Server-side OSRM proxy for map route geometries. The browser asks the backend instead of a public OSRM server,
/// so no client coordinate leaves the installation unless the operator allows routing. Answers Disabled without
/// any outbound call when the geo service policy switched routing off, NoRoute when OSRM finds no connection and
/// Unavailable on every other failure.
/// </summary>
/// <param name="httpClientFactory">Provides the named routing client (User-Agent and central outbound lock)</param>
/// <param name="geoServicesPolicy">Effective geo service configuration (on/off and OSRM base URL)</param>
/// <param name="logger">Logs unavailable OSRM answers on Warning; a disabled service is normal operation and not logged</param>

using System.Globalization;
using System.Text.Json;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.GeoServices;
using Klacks.Api.Domain.Interfaces.Routing;

namespace Klacks.Api.Infrastructure.Services.Routing;

public class OsrmRouteGeometryService : IOsrmRouteGeometryService
{
    private const string GeometryQuery = "?overview=full&geometries=geojson";
    private const string CoordinateSeparator = ",";
    private const string WaypointSeparator = ";";
    private const string CoordinateFormat = "F6";
    private const string CodeProperty = "code";
    private const string RoutesProperty = "routes";
    private const string GeometryProperty = "geometry";
    private const string CoordinatesProperty = "coordinates";
    private const string OkCode = "Ok";
    private const string NoRouteCode = "NoRoute";
    private const string NoSegmentCode = "NoSegment";
    private const int MinimumPositionLength = 2;
    private const int LongitudeIndex = 0;
    private const int LatitudeIndex = 1;

    private static readonly RouteGeometryResult Disabled = new(RouteGeometryStatus.Disabled, []);
    private static readonly RouteGeometryResult Unavailable = new(RouteGeometryStatus.Unavailable, []);
    private static readonly RouteGeometryResult NoRoute = new(RouteGeometryStatus.NoRoute, []);

    private readonly HttpClient _httpClient;
    private readonly IGeoServicesPolicy _geoServicesPolicy;
    private readonly ILogger<OsrmRouteGeometryService> _logger;

    public OsrmRouteGeometryService(
        IHttpClientFactory httpClientFactory,
        IGeoServicesPolicy geoServicesPolicy,
        ILogger<OsrmRouteGeometryService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(ExternalHttpClientConstants.RoutingClientName);
        _geoServicesPolicy = geoServicesPolicy;
        _logger = logger;
    }

    public async Task<RouteGeometryResult> GetRouteGeometryAsync(IReadOnlyList<RoutePoint> waypoints, CancellationToken cancellationToken)
    {
        if (!_geoServicesPolicy.Routing.IsEnabled)
        {
            return Disabled;
        }

        try
        {
            using var response = await _httpClient.GetAsync(BuildUrl(waypoints), cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var code = TryReadCode(body);

            if (code is NoRouteCode or NoSegmentCode)
            {
                return NoRoute;
            }

            if (!response.IsSuccessStatusCode || code != OkCode)
            {
                _logger.LogWarning(
                    "OSRM route request failed with {StatusCode} ({Code}), the caller has to fall back to straight lines.",
                    (int)response.StatusCode,
                    code);
                return Unavailable;
            }

            var points = ReadGeometry(body);
            if (points.Count == 0)
            {
                _logger.LogWarning("OSRM returned no route geometry, the caller has to fall back to straight lines.");
                return Unavailable;
            }

            return new RouteGeometryResult(RouteGeometryStatus.Ok, points);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OSRM route request failed, the caller has to fall back to straight lines.");
            return Unavailable;
        }
    }

    private string BuildUrl(IReadOnlyList<RoutePoint> waypoints)
    {
        var coordinates = string.Join(
            WaypointSeparator,
            waypoints.Select(w =>
                w.Lon.ToString(CoordinateFormat, CultureInfo.InvariantCulture)
                + CoordinateSeparator
                + w.Lat.ToString(CoordinateFormat, CultureInfo.InvariantCulture)));

        return $"{_geoServicesPolicy.Routing.BaseUrl}{GeoServicePaths.OsrmRouteDriving}/{coordinates}{GeometryQuery}";
    }

    private static string? TryReadCode(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty(CodeProperty, out var code)
                && code.ValueKind == JsonValueKind.String
                    ? code.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static List<RoutePoint> ReadGeometry(string body)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        if (!root.TryGetProperty(RoutesProperty, out var routes)
            || routes.ValueKind != JsonValueKind.Array
            || routes.GetArrayLength() == 0
            || !routes[0].TryGetProperty(GeometryProperty, out var geometry)
            || !geometry.TryGetProperty(CoordinatesProperty, out var coordinates)
            || coordinates.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return coordinates.EnumerateArray()
            .Where(position => position.ValueKind == JsonValueKind.Array && position.GetArrayLength() >= MinimumPositionLength)
            .Select(position => new RoutePoint(position[LatitudeIndex].GetDouble(), position[LongitudeIndex].GetDouble()))
            .ToList();
    }
}
