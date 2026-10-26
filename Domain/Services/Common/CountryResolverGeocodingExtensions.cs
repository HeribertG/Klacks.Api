// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the country name a geocoding query is sent with, shared by route optimization, container autofill, group
/// geocoding and the batch geocoding so none of them falls back to a hard-coded country. An empty country code means the company default country (APP_ADDRESS_COUNTRY); a code the
/// resolver does not know is passed on unchanged; without any known country the name is empty (no country filter).
/// The interactive address validation (AddressGeocodingValidator, validate_address) and the address backfill (AddressGeocodingFiller) keep
/// their own resolution, which falls back to the default country for an unknown code, and shares only GeocodingCountryName.
/// The resolver is scoped and reads the database, so callers must not run these lookups in parallel.
/// </summary>
/// <param name="resolver">Scoped country resolver (ISO code or any language name, default from the settings)</param>
/// <param name="countryCode">Country code or name stored on the address; empty or null for the company default</param>

using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Domain.Services.Common;

public static class CountryResolverGeocodingExtensions
{
    public static async Task<string> ResolveGeocodingNameAsync(
        this ICountryResolver resolver, string? countryCode, CancellationToken cancellationToken = default)
    {
        var country = await resolver.ResolveAsync(countryCode, cancellationToken);
        if (country == null && string.IsNullOrWhiteSpace(countryCode))
        {
            country = await resolver.GetDefaultAsync(cancellationToken);
        }

        var name = GeocodingCountryName.Of(country);
        if (!string.IsNullOrEmpty(name))
        {
            return name;
        }

        return string.IsNullOrWhiteSpace(countryCode) ? string.Empty : countryCode;
    }

    public static async Task<string> ResolveDefaultGeocodingNameAsync(
        this ICountryResolver resolver, CancellationToken cancellationToken = default)
    {
        var country = await resolver.GetDefaultAsync(cancellationToken);
        return GeocodingCountryName.Of(country);
    }
}
