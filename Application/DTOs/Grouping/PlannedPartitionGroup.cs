// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Grouping;

/// <summary>
/// One node (region, canton or city) GroupPartitionPlanner would create or reuse, in top-down order so a
/// parent always appears before its children.
/// </summary>
/// <param name="Key">Stable identity for this planning run (not a database id); referenced by ParentKey and by PartitionClientAssignment.LeafGroupKey.</param>
/// <param name="Name">Display name the group has or would get.</param>
/// <param name="ParentKey">Key of the planned parent node, or null when the node attaches directly under the caller-supplied root group (or the database root when none was given).</param>
/// <param name="Existed">True when a matching group (by name under the resolved parent) was already found in the database.</param>
/// <param name="ExistingGroupId">Id of the reused group when Existed is true; otherwise null.</param>
/// <param name="ClientCount">Number of clients that would become direct members of exactly this group (0 for a pure hierarchy node such as a region or, in canton_city mode, a canton).</param>
public sealed record PlannedPartitionGroup(
    string Key,
    string Name,
    string? ParentKey,
    bool Existed,
    Guid? ExistingGroupId,
    int ClientCount);
