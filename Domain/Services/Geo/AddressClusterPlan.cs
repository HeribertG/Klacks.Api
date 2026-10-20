// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.Geo;

/// <summary>
/// Result of AddressClusterPlanner.Plan.
/// </summary>
/// <param name="Clusters">Cluster centres ordered by country, state and city (ordinal).</param>
/// <param name="Assignments">One entry per placed address.</param>
/// <param name="Rejections">Addresses that could not be placed, with the reason.</param>
/// <param name="PlaceAttachments">Every non-centre place that joined a centre as a whole, ordered by country, state and place.</param>
/// <param name="StateThresholds">The centre threshold applied to every state, ordered by country and state.</param>
public sealed record AddressClusterPlan(
    IReadOnlyList<AddressCluster> Clusters,
    IReadOnlyList<AddressClusterAssignment> Assignments,
    IReadOnlyList<AddressClusterRejection> Rejections,
    IReadOnlyList<ClusterPlaceAttachment> PlaceAttachments,
    IReadOnlyList<ClusterStateThreshold> StateThresholds);
