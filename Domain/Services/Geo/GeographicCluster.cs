// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One cluster of the GeographicClusterer.
/// </summary>
/// <param name="Index">Position of the cluster, counted from 0 in the cluster order (west to east)</param>
/// <param name="MemberIds">Ids of the member points in ascending order; never empty</param>
/// <param name="CentroidLatitude">Mean latitude of the members in degrees</param>
/// <param name="CentroidLongitude">Mean longitude of the members in degrees</param>

namespace Klacks.Api.Domain.Services.Geo;

public sealed record GeographicCluster(
    int Index,
    IReadOnlyList<Guid> MemberIds,
    double CentroidLatitude,
    double CentroidLongitude);
