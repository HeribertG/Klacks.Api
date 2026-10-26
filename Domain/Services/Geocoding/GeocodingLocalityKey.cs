// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Normalized key of a locality in the geocoding cache: country code and postcode upper-cased, city lower-cased,
/// all trimmed with inner whitespace collapsed to one space, so "8001 ", "8001" and " Zürich" / "zürich" share one
/// Nominatim answer. Create returns null when postcode or city is blank, because such an address is never geocoded.
/// </summary>
/// <param name="CountryCode">Resolved country code of the address (empty when the installation knows none)</param>
/// <param name="PostalCode">Normalized postcode</param>
/// <param name="City">Normalized city</param>

namespace Klacks.Api.Domain.Services.Geocoding;

public sealed record GeocodingLocalityKey(string CountryCode, string PostalCode, string City)
{
    private static readonly char[] Whitespace = [' ', '\t', '\r', '\n', ' '];

    public static GeocodingLocalityKey? Create(string? countryCode, string? postalCode, string? city)
    {
        var zip = Collapse(postalCode).ToUpperInvariant();
        var place = Collapse(city).ToLowerInvariant();
        if (zip.Length == 0 || place.Length == 0)
        {
            return null;
        }

        return new GeocodingLocalityKey(Collapse(countryCode).ToUpperInvariant(), zip, place);
    }

    private static string Collapse(string? value) =>
        string.Join(' ', (value ?? string.Empty).Split(Whitespace, StringSplitOptions.RemoveEmptyEntries));
}
