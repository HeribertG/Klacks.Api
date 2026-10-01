// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Names and identification of the named HTTP clients used for public OpenStreetMap-based services.
/// Public OSRM and Nominatim servers reject requests without a User-Agent (HTTP 403).
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ExternalHttpClientConstants
{
    public const string UserAgent = "Klacks Application (contact: admin@klacks.com)";
    public const string NominatimClientName = "Nominatim";
    public const string RoutingClientName = "Routing";
}
