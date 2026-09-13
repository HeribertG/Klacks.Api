// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Granularity at which partition_clients_by_address groups clients by their resolved address.
/// State is the country's middle administrative level (state, province, Bundesland, département).
/// Cluster nests density-based city clusters under the state group.
/// </summary>
public enum GroupPartitionLevelEnum
{
    State,
    City,
    StateCity,
    Cluster
}
