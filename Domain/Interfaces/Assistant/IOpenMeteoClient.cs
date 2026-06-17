// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Fetches weather for a coordinate from the public Open-Meteo API (no API key required).
/// </summary>
public interface IOpenMeteoClient
{
    /// <summary>
    /// Returns one of Klacksy's weather i18n keys for the current conditions, or empty string on any
    /// failure (Open-Meteo down, invalid coordinates, timeout) — weather is always optional for the greeting.
    /// </summary>
    Task<string> GetWeatherKeyAsync(double latitude, double longitude, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a structured current-weather snapshot plus a short daily forecast for the coordinate,
    /// or null on any failure. Conditions are English WMO descriptions so the LLM can phrase them in
    /// the user's language.
    /// </summary>
    /// <param name="latitude">Latitude in decimal degrees</param>
    /// <param name="longitude">Longitude in decimal degrees</param>
    /// <param name="forecastDays">Number of daily forecast entries to include (clamped to 1..7)</param>
    Task<WeatherSnapshot?> GetCurrentWeatherAsync(double latitude, double longitude, int forecastDays = 3, CancellationToken cancellationToken = default);
}
