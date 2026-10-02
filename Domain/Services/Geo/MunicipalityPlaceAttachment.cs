// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.Geo;

/// <summary>
/// Where a small place of a city cluster (too few addresses for its own sub-cluster) ended up.
/// </summary>
/// <param name="Place">Canonical name of the small place.</param>
/// <param name="SubClusterCity">Sub-cluster the place joined; null when it stays a direct member of the city cluster.</param>
/// <param name="ClientCount">Addresses of the place.</param>
/// <param name="DistanceKm">Distance from the place's mean position to the joined sub-cluster centre; null when it stays.</param>
public sealed record MunicipalityPlaceAttachment(
    string Place,
    string? SubClusterCity,
    int ClientCount,
    double? DistanceKm);
