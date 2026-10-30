// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One located item (for example a customer) the GeographicClusterer assigns to a cluster.
/// </summary>
/// <param name="Id">Stable id of the item; ties are broken by it, so it must be unique in one clustering</param>
/// <param name="Latitude">Latitude in degrees</param>
/// <param name="Longitude">Longitude in degrees</param>

namespace Klacks.Api.Domain.Services.Geo;

public readonly record struct GeographicClusterPoint(Guid Id, double Latitude, double Longitude);
