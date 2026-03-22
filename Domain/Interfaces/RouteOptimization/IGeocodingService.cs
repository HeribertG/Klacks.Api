// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Service for address geocoding (coordinate resolution and address validation).
/// </summary>
/// <param name="city">City for simple geocoding</param>
/// <param name="fullAddress">Full address for precise geocoding</param>
/// <param name="country">Country for geocoding</param>

namespace Klacks.Api.Domain.Interfaces.RouteOptimization;

public interface IGeocodingService
{
    Task<(double? Latitude, double? Longitude)> GeocodeAsync(string city, string country, CancellationToken cancellationToken = default);
    Task<(double? Latitude, double? Longitude)> GeocodeAddressAsync(string fullAddress, string country, CancellationToken cancellationToken = default);
    Task<GeocodingValidationResult> ValidateExactAddressAsync(string? street, string postalCode, string city, string country);
    Task<List<AddressSuggestion>> GetAddressSuggestionsAsync(string? street, string postalCode, string city, string country, int limit = 5);
}

public class GeocodingValidationResult
{
    public bool Found { get; set; }
    public bool ExactMatch { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ReturnedAddress { get; set; }
    public string? MatchType { get; set; }
    public string? State { get; set; }
}

public class AddressSuggestion
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
