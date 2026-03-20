// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Antwort-DTO fuer die Adress-Validierung mit Geocoding-Ergebnis und Alternativ-Vorschlaegen.
/// </summary>
/// <param name="IsValid">Gibt an ob die Adresse geocodiert werden konnte</param>
/// <param name="Suggestions">Liste alternativer Adressen bei ungueltigem Ergebnis</param>

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
