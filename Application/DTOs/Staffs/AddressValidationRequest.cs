// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Request DTO for address validation. Only fields relevant for geocoding.
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
