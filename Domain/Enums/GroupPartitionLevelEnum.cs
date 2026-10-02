// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Granularity at which partition_clients_by_address groups clients by their resolved address.
/// State is the country's middle administrative level (state, province, Bundesland, département).
/// Cluster nests density-based city clusters under the state group. ClusterMunicipality adds a fourth level:
/// inside every city cluster, places outside the centre city that hold enough of the cluster's addresses become
/// sub-cluster groups, nearby smaller places join them, and centre-city clients stay in the cluster group.
/// </summary>
public enum GroupPartitionLevelEnum
{
    State,
    City,
    StateCity,
    Cluster,
    ClusterMunicipality
}
