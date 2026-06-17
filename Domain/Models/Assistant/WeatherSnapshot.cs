// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

public sealed class WeatherSnapshot
{
    public double TemperatureCelsius { get; set; }

    public double WindSpeedKmh { get; set; }

    public int WeatherCode { get; set; }

    public string Condition { get; set; } = string.Empty;

    public IReadOnlyList<WeatherDailyForecast> Forecast { get; set; } = [];
}

public sealed class WeatherDailyForecast
{
    public string Date { get; set; } = string.Empty;

    public double TemperatureMaxCelsius { get; set; }

    public double TemperatureMinCelsius { get; set; }

    public int WeatherCode { get; set; }

    public string Condition { get; set; } = string.Empty;
}
