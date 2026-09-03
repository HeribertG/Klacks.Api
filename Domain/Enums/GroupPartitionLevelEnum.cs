// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Granularity at which partition_clients_by_address groups clients by their resolved address.
/// </summary>
public enum GroupPartitionLevelEnum
{
    Canton,
    City,
    CantonCity
}
