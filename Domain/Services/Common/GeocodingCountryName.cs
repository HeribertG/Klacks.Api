// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one naming rule for the country a geocoding query is sent with: German name, then English name, then the ISO
/// code. Nominatim treats its country parameter as free text, not as an ISO code, so a name finds more hits.
/// </summary>
/// <param name="country">Resolved country, or null when none is known (yields an empty name)</param>

using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Services.Common;

public static class GeocodingCountryName
{
    public static string Of(Countries? country) =>
        country?.Name?.De ?? country?.Name?.En ?? country?.Abbreviation ?? string.Empty;
}
