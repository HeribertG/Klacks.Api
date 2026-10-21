// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
    Task<GeocodingSearchResult> SearchAsync(string query, string? language, CancellationToken cancellationToken = default);
    bool IsEnabled { get; }
}
