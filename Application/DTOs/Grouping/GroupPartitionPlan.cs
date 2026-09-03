// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Grouping;

/// <summary>
/// Read-only result of GroupPartitionPlanner.Plan: the group hierarchy it would build or reuse, the
/// clients it would place into the leaf groups, and the clients it could not place.
/// </summary>
/// <param name="TotalClients">Total number of clients of the requested entity type considered.</param>
/// <param name="SkippedAlreadyGroupedCount">Clients skipped because they already hold an active (non-scenario) group membership and includeAlreadyGrouped was false.</param>
/// <param name="Groups">Planned groups in top-down order (a parent always precedes its children).</param>
/// <param name="Assignments">Planned client-to-leaf-group placements.</param>
/// <param name="Unassignable">Clients that cannot be placed at the requested level, with the reason.</param>
/// <param name="Warnings">Non-fatal issues worth surfacing in a preview, e.g. a planned group name that already exists elsewhere in the tree under a different parent.</param>
public sealed record GroupPartitionPlan(
    int TotalClients,
    int SkippedAlreadyGroupedCount,
    IReadOnlyList<PlannedPartitionGroup> Groups,
    IReadOnlyList<PartitionClientAssignment> Assignments,
    IReadOnlyList<UnassignablePartitionClient> Unassignable,
    IReadOnlyList<string> Warnings);
