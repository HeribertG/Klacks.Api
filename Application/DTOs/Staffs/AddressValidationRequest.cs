// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Request-DTO fuer die Adress-Validierung. Nur die fuer Geocoding relevanten Felder.
/// </summary>

namespace Klacks.Api.Application.DTOs.Staffs;

public class AddressValidationRequest
{
    public string? Street { get; set; }
    public string? Zip { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
}
