// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One conditional coordinate write: it lands only while the address is live, still has neither latitude nor
/// longitude and still carries exactly the postcode, city and country that were geocoded, so a user edit made in
/// between always wins. The state is set only when the address has none.
/// </summary>
/// <param name="StateIfEmpty">State abbreviation for an address without a state, or null to leave the state alone</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Geocoding;

public sealed record AddressGeocodingFill(
    Guid AddressId,
    string ExpectedZip,
    string ExpectedCity,
    string ExpectedCountry,
    double Latitude,
    double Longitude,
    string? StateIfEmpty,
    AddressGeocodingSource Source,
    AddressGeocodingAttribution Attribution,
    DateTime At);
