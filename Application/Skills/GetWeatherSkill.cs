// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Returns current weather and a short daily forecast for a location via the public Open-Meteo API
/// (no API key, no web search provider needed). Resolves the location from explicit coordinates, else
/// from a city name via geocoding, else falls back to the configured company location.
/// </summary>
/// <param name="city">Optional city name to look up (e.g. "Bern"). Geocoded when no coordinates are given.</param>
/// <param name="country">Optional country to disambiguate the city during geocoding.</param>
/// <param name="latitude">Optional explicit latitude in decimal degrees (used together with longitude).</param>
/// <param name="longitude">Optional explicit longitude in decimal degrees (used together with latitude).</param>
/// <param name="forecastDays">Optional number of daily forecast entries (1..7, default 3).</param>

using System.Globalization;
using System.Text.Json;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_weather")]
public class GetWeatherSkill : BaseSkillImplementation
{
    private const string CityParam = "city";
    private const string CountryParam = "country";
    private const string LatitudeParam = "latitude";
    private const string LongitudeParam = "longitude";
    private const string ForecastDaysParam = "forecastDays";
    private const int DefaultForecastDays = 3;

    private readonly IOpenMeteoClient _weatherClient;
    private readonly IGeocodingService _geocodingService;
    private readonly ICompanyLocationProvider _companyLocationProvider;

    public GetWeatherSkill(
        IOpenMeteoClient weatherClient,
        IGeocodingService geocodingService,
        ICompanyLocationProvider companyLocationProvider)
    {
        _weatherClient = weatherClient;
        _geocodingService = geocodingService;
        _companyLocationProvider = companyLocationProvider;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var city = GetParameter<string>(parameters, CityParam);
        var country = GetParameter<string>(parameters, CountryParam) ?? string.Empty;
        var latitude = ReadDouble(parameters, LatitudeParam);
        var longitude = ReadDouble(parameters, LongitudeParam);
        var forecastDays = ReadInt(parameters, ForecastDaysParam) ?? DefaultForecastDays;

        double lat;
        double lon;
        string locationLabel;

        if (latitude.HasValue && longitude.HasValue)
        {
            lat = latitude.Value;
            lon = longitude.Value;
            locationLabel = !string.IsNullOrWhiteSpace(city) ? city! : FormatCoordinates(lat, lon);
        }
        else if (!string.IsNullOrWhiteSpace(city))
        {
            var (geoLat, geoLon) = await _geocodingService.GeocodeAsync(city!, country, cancellationToken);
            if (geoLat is null || geoLon is null)
            {
                return SkillResult.Error($"Could not find coordinates for location '{city}'. Try a different spelling or add a country.");
            }

            lat = geoLat.Value;
            lon = geoLon.Value;
            locationLabel = city!;
        }
        else
        {
            var companyLocation = await _companyLocationProvider.GetCompanyLocationAsync(cancellationToken);
            if (companyLocation is null)
            {
                return SkillResult.Error("No location was provided and no company location is configured. Ask the user which city to check.");
            }

            lat = companyLocation.Value.Latitude;
            lon = companyLocation.Value.Longitude;
            locationLabel = "the company location";
        }

        var snapshot = await _weatherClient.GetCurrentWeatherAsync(lat, lon, forecastDays, cancellationToken);
        if (snapshot is null)
        {
            return SkillResult.Error("Weather data is currently unavailable (Open-Meteo did not respond).");
        }

        var summary = string.Create(
            CultureInfo.InvariantCulture,
            $"Current weather at {locationLabel}: {snapshot.TemperatureCelsius:0.#}°C, {snapshot.Condition}, wind {snapshot.WindSpeedKmh:0.#} km/h.");

        return SkillResult.SuccessResult(
            new
            {
                Location = locationLabel,
                Latitude = lat,
                Longitude = lon,
                Current = new
                {
                    snapshot.TemperatureCelsius,
                    snapshot.WindSpeedKmh,
                    snapshot.Condition,
                },
                snapshot.Forecast,
            },
            summary);
    }

    private static string FormatCoordinates(double latitude, double longitude) =>
        string.Create(CultureInfo.InvariantCulture, $"{latitude:0.##}, {longitude:0.##}");

    private static double? ReadDouble(Dictionary<string, object> parameters, string name)
    {
        if (!parameters.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        if (value is JsonElement json)
        {
            return json.ValueKind == JsonValueKind.Number && json.TryGetDouble(out var parsed) ? parsed : null;
        }

        return GetParameter<double?>(parameters, name);
    }

    private static int? ReadInt(Dictionary<string, object> parameters, string name)
    {
        if (!parameters.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        if (value is JsonElement json)
        {
            return json.ValueKind == JsonValueKind.Number && json.TryGetInt32(out var parsed) ? parsed : null;
        }

        return GetParameter<int?>(parameters, name);
    }
}
