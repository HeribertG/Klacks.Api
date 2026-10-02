// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

/// <summary>
/// A small place of a city cluster that is too small for its own municipality sub-cluster, and the group its
/// clients were placed in: the nearest sub-cluster when that is nearer than the cluster centre, otherwise the
/// city cluster itself.
/// </summary>
/// <param name="Place">Name of the small place.</param>
/// <param name="ClusterName">Name of the city cluster the place belongs to.</param>
/// <param name="TargetGroupName">Name of the group its clients join (a sub-cluster or the city cluster).</param>
/// <param name="JoinedSubCluster">True when the place joined a municipality sub-cluster.</param>
/// <param name="ClientCount">Number of clients living in the place.</param>
/// <param name="DistanceKm">Distance to the joined sub-cluster centre; null when the place stays in the city cluster.</param>
public sealed record PartitionPlaceAttachment(
    string Place,
    string ClusterName,
    string TargetGroupName,
    bool JoinedSubCluster,
    int ClientCount,
    double? DistanceKm);
