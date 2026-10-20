// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Groups;

/// <summary>
/// One region, state/province or city group in a partition_clients_by_address preview or apply result, in
/// top-down order (a parent always precedes its children).
/// </summary>
/// <param name="Name">Display name the group has or would get.</param>
/// <param name="ParentName">Name of the parent group, or null when it attaches directly under the caller-supplied root group (or the database root when none was given).</param>
/// <param name="Existed">True when a matching group already existed and was reused instead of created.</param>
/// <param name="GroupId">Id of the group; set for a reused group in preview, and for every group (reused or newly created) after apply.</param>
/// <param name="ClientCount">Number of clients this run places (or placed) as direct members of exactly this group; clients that already held a membership and were not re-placed are not counted.</param>
/// <param name="HasCoordinates">True when the group gets (cluster or sub-cluster centre) coordinates from this run; always false for region, state and city nodes.</param>
/// <param name="DirectCount">Of ClientCount: clients placed by the name of their own city (the centre city of a cluster); equals ClientCount for state and city nodes.</param>
/// <param name="AttachedCount">Of ClientCount: clients of other places that joined as a whole by distance.</param>
/// <param name="AttachedPlaceCount">Number of places whose clients joined this group by distance.</param>
/// <param name="Method">How the members of this group were chosen, in words.</param>
public sealed record PartitionGroupSummary(
    string Name,
    string? ParentName,
    bool Existed,
    Guid? GroupId,
    int ClientCount,
    bool HasCoordinates = false,
    int DirectCount = 0,
    int AttachedCount = 0,
    int AttachedPlaceCount = 0,
    string Method = "");
