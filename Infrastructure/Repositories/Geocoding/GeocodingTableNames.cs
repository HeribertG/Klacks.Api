// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Table names of the geocoding bookkeeping, shared by the EF configuration and the upsert SQL of the repositories.
/// </summary>

namespace Klacks.Api.Infrastructure.Repositories.Geocoding;

public static class GeocodingTableNames
{
    public const string AddressGeocodingState = "address_geocoding_state";

    public const string GeocodedLocality = "geocoded_locality";
}
