// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one acceptance rule of a geocoding hit for a client address, shared by the interactive address
/// validation and the background geocoding: only a hit that is exact for the street is accepted, or any
/// hit when the address has no street. The country name of the query comes from GeocodingCountryName.
/// </summary>

using Klacks.Api.Domain.Interfaces.RouteOptimization;

namespace Klacks.Api.Application.Services.Geocoding;

public static class AddressGeocodingRules
{
    public const string ExactMatchType = "exact";

    public static bool IsAcceptedHit(GeocodingValidationResult result, string? street) =>
        result.Found
        && (string.IsNullOrWhiteSpace(street) || result.ExactMatch || result.MatchType == ExactMatchType);
}
