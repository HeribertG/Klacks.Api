// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// English skill messages for geo services that the operator switched off via the GeoServices configuration.
/// </summary>

namespace Klacks.Api.Application.Constants;

public static class GeoServiceMessages
{
    public const string GeocodingDisabled =
        "Geocoding is disabled by operator configuration, so no address or place can be resolved to coordinates. " +
        "This says nothing about whether the address or place is correct.";

    public const string GroupGeocodingDisabled =
        "Geocoding is disabled by operator configuration, so groups cannot receive coordinates automatically. " +
        "Groups that were not processed yet stay pending and are processed after geocoding is switched back on.";

    public const string OpenRouteServiceDisabled =
        "OpenRouteService is disabled by operator configuration";
}
