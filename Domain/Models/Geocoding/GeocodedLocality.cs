// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Persistent geocoding cache of one locality (country code, postcode, city in their normalized form,
/// GeocodingLocalityKey): the centroid Nominatim returned for the postcode and city, or the fact that it found
/// nothing (Found false, retried after the not-found window). State and county are kept raw so the state of an
/// address can be filled from a cache hit exactly as from a fresh answer. Street-level hits are never stored here.
/// </summary>

namespace Klacks.Api.Domain.Models.Geocoding;

public class GeocodedLocality
{
    public string CountryCode { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public bool Found { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? State { get; set; }

    public string? County { get; set; }

    public DateTime ResolvedAt { get; set; }
}
