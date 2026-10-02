// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.Geo;

/// <summary>
/// Result of MunicipalitySubClusterPlanner for one city cluster.
/// </summary>
/// <param name="SubClusters">Sub-clusters ordered by name (ordinal).</param>
/// <param name="SubClusterByClientId">Sub-cluster name per client that moves out of the city cluster group; clients missing here stay direct members of the city cluster.</param>
/// <param name="Attachments">Every small place of the cluster with the group it ended up in, ordered by name.</param>
public sealed record MunicipalitySubClusterPlan(
    IReadOnlyList<MunicipalitySubCluster> SubClusters,
    IReadOnlyDictionary<Guid, string> SubClusterByClientId,
    IReadOnlyList<MunicipalityPlaceAttachment> Attachments);
