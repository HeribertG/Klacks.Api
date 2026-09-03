// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Groups;

/// <summary>
/// One region, canton or city group in a partition_clients_by_address preview or apply result, in
/// top-down order (a parent always precedes its children).
/// </summary>
/// <param name="Name">Display name the group has or would get.</param>
/// <param name="ParentName">Name of the parent group, or null when it attaches directly under the caller-supplied root group (or the database root when none was given).</param>
/// <param name="Existed">True when a matching group already existed and was reused instead of created.</param>
/// <param name="GroupId">Id of the group; set for a reused group in preview, and for every group (reused or newly created) after apply.</param>
/// <param name="ClientCount">Number of clients that would become (or became) direct members of exactly this group.</param>
public sealed record PartitionGroupSummary(
    string Name,
    string? ParentName,
    bool Existed,
    Guid? GroupId,
    int ClientCount);
