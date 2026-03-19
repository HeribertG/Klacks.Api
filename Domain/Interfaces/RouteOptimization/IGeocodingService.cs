// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Service fuer Adress-Geocodierung (Koordinaten-Aufloesung und Adress-Validierung).
/// </summary>
/// <param name="city">Stadt fuer einfache Geocodierung</param>
/// <param name="fullAddress">Vollstaendige Adresse fuer praezise Geocodierung</param>
/// <param name="country">Land fuer die Geocodierung</param>

namespace Klacks.Api.Domain.Interfaces.RouteOptimization;

public interface IGeocodingService
{
    Task<(double? Latitude, double? Longitude)> GeocodeAsync(string city, string country, CancellationToken cancellationToken = default);
    Task<(double? Latitude, double? Longitude)> GeocodeAddressAsync(string fullAddress, string country, CancellationToken cancellationToken = default);
    Task<GeocodingValidationResult> ValidateExactAddressAsync(string? street, string postalCode, string city, string country);
}

public class GeocodingValidationResult
{
    public bool Found { get; set; }
    public bool ExactMatch { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ReturnedAddress { get; set; }
    public string? MatchType { get; set; }
}
