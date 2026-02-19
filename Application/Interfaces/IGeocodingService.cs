namespace Klacks.Api.Application.Interfaces;

public interface IGeocodingService
{
    Task<(double? Latitude, double? Longitude)> GeocodeAsync(string city, string country);
    Task<(double? Latitude, double? Longitude)> GeocodeAddressAsync(string fullAddress, string country);
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
