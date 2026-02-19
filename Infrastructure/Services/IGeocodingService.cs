namespace Klacks.Api.Infrastructure.Services;

public interface IGeocodingService
{
    Task<(double? Latitude, double? Longitude)> GeocodeAsync(string city, string country);
    Task<(double? Latitude, double? Longitude)> GeocodeAddressAsync(string fullAddress, string country);
    Task<GeocodingValidationResult> ValidateExactAddressAsync(string? street, string postalCode, string city, string country);
}
