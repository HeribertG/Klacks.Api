// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Selection of the background scan: live addresses without coordinates with postcode and city whose owner may
/// be geocoded, that were requested, never attempted, attempted for a different postcode/city/country, or whose
/// not-found or unavailable attempt is older than its retry window. Requested addresses come first.
/// </summary>
/// <param name="IncludeEmployeeAddresses">True when the installation allows employee addresses to be geocoded</param>
/// <param name="NotFoundRetryBefore">A not-found attempt before this instant is due again</param>
/// <param name="UnavailableRetryBefore">An unavailable attempt before this instant is due again</param>
/// <param name="Limit">Most candidates returned</param>

namespace Klacks.Api.Application.DTOs.Geocoding;

public sealed record AddressGeocodingDueQuery(
    bool IncludeEmployeeAddresses,
    DateTime NotFoundRetryBefore,
    DateTime UnavailableRetryBefore,
    int Limit);
