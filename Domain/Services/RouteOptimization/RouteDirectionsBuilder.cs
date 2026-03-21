// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Retrieves turn-by-turn route directions from OSRM for each segment in a route.
/// </summary>
/// <param name="_httpClient">HTTP client for OSRM API calls</param>
/// <param name="_logger">Logger for diagnostic output</param>

using System.Text.Json;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Staffs;

namespace Klacks.Api.Domain.Services.RouteOptimization;

public class RouteDirectionsBuilder : IRouteDirectionsBuilder
{
    private readonly ILogger<RouteDirectionsBuilder> _logger;
    private readonly HttpClient _httpClient;
    private const string OSRM_BASE_URL = "https://router.project-osrm.org";

    public RouteDirectionsBuilder(
        ILogger<RouteDirectionsBuilder> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<List<RouteSegmentDirections>> GetRouteDirectionsAsync(
        List<Location> fullRoute,
        DistanceMatrix distanceMatrix,
        ContainerTransportMode containerTransportMode)
    {
        var segmentDirections = new List<RouteSegmentDirections>();

        for (int i = 0; i < fullRoute.Count - 1; i++)
        {
            var fromLocation = fullRoute[i];
            var toLocation = fullRoute[i + 1];

            var transportMode = containerTransportMode == ContainerTransportMode.Mix
                ? toLocation.TransportMode
                : (TransportMode)(int)containerTransportMode;

            var profile = GetOsrmProfileFromTransportMode(transportMode);
            var transportModeText = GetTransportModeText(transportMode);

            try
            {
                var directions = await GetOsrmDirectionsAsync(fromLocation, toLocation, profile);
                directions = directions with
                {
                    FromName = fromLocation.Name,
                    ToName = toLocation.Name,
                    TransportMode = transportModeText
                };
                segmentDirections.Add(directions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get directions for segment {From} -> {To}", fromLocation.Name, toLocation.Name);
                segmentDirections.Add(new RouteSegmentDirections(
                    fromLocation.Name,
                    toLocation.Name,
                    transportModeText,
                    0,
                    TimeSpan.Zero,
                    new List<DirectionStep>()));
            }
        }

        return segmentDirections;
    }

    private async Task<RouteSegmentDirections> GetOsrmDirectionsAsync(Location from, Location to, string profile)
    {
        var coordinates = $"{from.Longitude:F6},{from.Latitude:F6};{to.Longitude:F6},{to.Latitude:F6}";
        var url = $"{OSRM_BASE_URL}/route/v1/{profile}/{coordinates}?steps=true&overview=false";

        _logger.LogInformation("Requesting OSRM directions: {Url}", url);

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.GetProperty("code").GetString() != "Ok")
        {
            throw new Exception($"OSRM API returned error: {root.GetProperty("code").GetString()}");
        }

        var route = root.GetProperty("routes")[0];
        var leg = route.GetProperty("legs")[0];
        var distanceMeters = leg.GetProperty("distance").GetDouble();
        var durationSeconds = leg.GetProperty("duration").GetDouble();

        var steps = new List<DirectionStep>();
        foreach (var step in leg.GetProperty("steps").EnumerateArray())
        {
            var maneuver = step.GetProperty("maneuver");
            var maneuverType = maneuver.GetProperty("type").GetString() ?? "";
            var modifier = maneuver.TryGetProperty("modifier", out var mod) ? mod.GetString() ?? "" : "";

            var streetName = step.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "";
            var stepDistance = step.GetProperty("distance").GetDouble();
            var stepDuration = (int)step.GetProperty("duration").GetDouble();

            var instruction = GenerateInstruction(maneuverType, modifier, streetName);

            if (!string.IsNullOrEmpty(instruction) && maneuverType != "arrive")
            {
                steps.Add(new DirectionStep(
                    instruction,
                    streetName,
                    stepDistance,
                    stepDuration,
                    maneuverType));
            }
        }

        return new RouteSegmentDirections(
            "",
            "",
            "",
            distanceMeters / 1000.0,
            TimeSpan.FromSeconds(durationSeconds),
            steps);
    }

    private static string GenerateInstruction(string maneuverType, string modifier, string streetName)
    {
        var streetPart = string.IsNullOrEmpty(streetName) ? "" : $" auf {streetName}";

        return maneuverType switch
        {
            "depart" => $"Starten{streetPart}",
            "turn" => modifier switch
            {
                "left" => $"Links abbiegen{streetPart}",
                "right" => $"Rechts abbiegen{streetPart}",
                "slight left" => $"Leicht links abbiegen{streetPart}",
                "slight right" => $"Leicht rechts abbiegen{streetPart}",
                "sharp left" => $"Scharf links abbiegen{streetPart}",
                "sharp right" => $"Scharf rechts abbiegen{streetPart}",
                "uturn" => $"Wenden{streetPart}",
                _ => $"Abbiegen{streetPart}"
            },
            "new name" => $"Weiter{streetPart}",
            "continue" => $"Geradeaus{streetPart}",
            "merge" => $"Einfädeln{streetPart}",
            "on ramp" => $"Auffahrt nehmen{streetPart}",
            "off ramp" => $"Abfahrt nehmen{streetPart}",
            "fork" => modifier switch
            {
                "left" => $"Links halten{streetPart}",
                "right" => $"Rechts halten{streetPart}",
                _ => $"Gabelung{streetPart}"
            },
            "end of road" => modifier switch
            {
                "left" => $"Am Ende links{streetPart}",
                "right" => $"Am Ende rechts{streetPart}",
                _ => $"Straßenende{streetPart}"
            },
            "roundabout" => $"Im Kreisverkehr{streetPart}",
            "rotary" => $"Im Kreisverkehr{streetPart}",
            "roundabout turn" => $"Kreisverkehr verlassen{streetPart}",
            "exit roundabout" => $"Kreisverkehr verlassen{streetPart}",
            "notification" => "",
            "arrive" => "Ziel erreicht",
            _ => ""
        };
    }

    private static string GetTransportModeText(TransportMode transportMode)
    {
        return transportMode switch
        {
            TransportMode.ByCar => "mit dem Auto",
            TransportMode.ByBicycle => "mit dem Fahrrad",
            TransportMode.ByFoot => "zu Fuss",
            _ => "mit dem Auto"
        };
    }

    private static string GetOsrmProfileFromTransportMode(TransportMode transportMode)
    {
        return transportMode switch
        {
            TransportMode.ByCar => "driving",
            TransportMode.ByBicycle => "cycling",
            TransportMode.ByFoot => "foot",
            _ => "driving"
        };
    }
}
