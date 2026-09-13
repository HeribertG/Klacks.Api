// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Grouping;

/// <summary>
/// One node (region, state, city or cluster) GroupPartitionPlanner would create or reuse, in top-down
/// order so a parent always appears before its children.
/// </summary>
/// <param name="Key">Stable identity for this planning run (not a database id); referenced by ParentKey and by PartitionClientAssignment.LeafGroupKey.</param>
/// <param name="Name">Display name the group has or would get.</param>
/// <param name="ParentKey">Key of the planned parent node, or null when the node attaches directly under the caller-supplied root group (or the database root when none was given).</param>
/// <param name="Existed">True when a matching group (by name under the resolved parent) was already found in the database.</param>
/// <param name="ExistingGroupId">Id of the reused group when Existed is true; otherwise null.</param>
/// <param name="ClientCount">Number of clients that would become direct members of exactly this group (0 for a pure hierarchy node such as a region or, in state_city or cluster mode, a state).</param>
/// <param name="Description">Description the group gets when created (state display name for a state node; empty otherwise).</param>
/// <param name="Latitude">Latitude written to a newly created cluster group (mean of its addresses); null for every other node.</param>
/// <param name="Longitude">Longitude written to a newly created cluster group; null for every other node.</param>
public sealed record PlannedPartitionGroup(
    string Key,
    string Name,
    string? ParentKey,
    bool Existed,
    Guid? ExistingGroupId,
    int ClientCount,
    string Description = "",
    double? Latitude = null,
    double? Longitude = null);
