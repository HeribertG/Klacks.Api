namespace Klacks.Api.Infrastructure.Services;

public class GeocodingValidationResult
{
    public bool Found { get; set; }
    public bool ExactMatch { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ReturnedAddress { get; set; }
    public string? MatchType { get; set; }
}
