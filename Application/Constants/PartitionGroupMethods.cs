// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Constants;

/// <summary>
/// How the members of a group planned by partition_clients_by_address were chosen; shown per group in the
/// skill result so the model can state the method instead of guessing it.
/// </summary>
public static class PartitionGroupMethods
{
    public const string Region = "region from the country's region file (holds states only)";

    public const string State = "state/province code of the address";

    public const string City = "city name of the address";

    public const string Cluster =
        "cluster centre: clients of the centre city by city name, other places as a whole to the nearest centre";

    public const string Municipality =
        "municipality sub-cluster: clients of the place by name, smaller places as a whole when nearer than the cluster centre";
}
