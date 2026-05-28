// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Fetches the current weather for a coordinate from Open-Meteo and maps the WMO weather code
/// to one of Klacksy's weather i18n keys. Returns empty string on any failure (Open-Meteo down,
/// invalid coordinates, timeout) — weather is always optional for the greeting.
/// </summary>
public interface IOpenMeteoClient
{
    Task<string> GetWeatherKeyAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
