// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Response DTO for address validation with geocoding result and alternative suggestions.
/// </summary>
/// <param name="IsValid">Indicates whether the address could be geocoded</param>
/// <param name="Suggestions">List of alternative addresses when the result is invalid</param>

using Klacks.Api.Domain.Interfaces.RouteOptimization;

namespace Klacks.Api.Application.DTOs.Staffs;

public class AddressValidationResponse
{
    public bool IsValid { get; set; }
    public string MatchType { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ReturnedAddress { get; set; }
    public string? ExpectedState { get; set; }
    public List<AddressSuggestionDto> Suggestions { get; set; } = [];
}

public class AddressSuggestionDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
