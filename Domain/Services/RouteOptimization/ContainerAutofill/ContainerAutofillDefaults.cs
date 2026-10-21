// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Fixed values of the container autofill: geocoding country fallback, location labels and sentinels.
/// </summary>

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

public static class ContainerAutofillDefaults
{
    public const string GeocodingCountry = "Switzerland";

    public const string UnknownLocationName = "Unknown";

    public const string StartBaseLabel = "StartBase";

    public const string EndBaseLabel = "EndBase";

    public const int NoCandidate = -1;
}
