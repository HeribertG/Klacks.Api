// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.Geo;

/// <summary>
/// One municipality sub-cluster inside a city cluster, named after its centre place.
/// </summary>
/// <param name="City">Canonical name of the centre place; also the name of the sub-cluster group.</param>
/// <param name="Latitude">Mean latitude of the centre place's addresses; null when none has coordinates.</param>
/// <param name="Longitude">Mean longitude of the centre place's addresses; null when none has coordinates.</param>
/// <param name="DirectCount">Addresses located in the centre place itself.</param>
/// <param name="AttachedCount">Addresses of smaller places attached to this sub-cluster by distance.</param>
public sealed record MunicipalitySubCluster(
    string City,
    double? Latitude,
    double? Longitude,
    int DirectCount,
    int AttachedCount);
