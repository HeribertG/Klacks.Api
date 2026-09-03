// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Grouping;

namespace Klacks.Api.Application.DTOs.Groups;

/// <summary>
/// Result of partitioning clients into a region/canton/city group hierarchy built from their address.
/// With Applied=false it is a dry-run preview; with Applied=true the groups and memberships were
/// persisted and the new memberships were re-read for verification. UnassignableSample is deliberately
/// truncated (see UnassignableCount for the true total) so a run over thousands of clients still returns
/// a small payload; Groups is not truncated because a run only ever plans a few dozen groups.
/// </summary>
/// <param name="Applied">False for a dry-run preview, true when groups and memberships were persisted.</param>
/// <param name="Level">The requested partition level (canton, city or canton_city).</param>
/// <param name="EntityType">The client type that was partitioned (Employee or ExternEmp).</param>
/// <param name="TotalClients">Total number of clients of the requested entity type considered.</param>
/// <param name="SkippedAlreadyGroupedCount">Clients skipped because they already hold an active group membership and includeAlreadyGrouped was false.</param>
/// <param name="UnassignableCount">Total number of clients that could not be placed (their address is missing the field(s) the level needs).</param>
/// <param name="AssignedCount">Number of new memberships created (only meaningful when Applied is true); 0 in a preview.</param>
/// <param name="VerifiedCount">Number of created memberships re-read and confirmed in the database (only meaningful when Applied is true).</param>
/// <param name="AlreadyMemberCount">Placements that already existed as a membership and were left untouched (relevant on a repeated apply).</param>
/// <param name="Groups">Planned or created groups, top-down.</param>
/// <param name="UnassignableSample">A truncated sample of the unassignable clients; see UnassignableCount for the true total.</param>
/// <param name="Warnings">Non-fatal issues worth surfacing, e.g. a planned group name that already exists elsewhere in the tree.</param>
public sealed record PartitionClientsByAddressResult(
    bool Applied,
    string Level,
    string EntityType,
    int TotalClients,
    int SkippedAlreadyGroupedCount,
    int UnassignableCount,
    int AssignedCount,
    int VerifiedCount,
    int AlreadyMemberCount,
    IReadOnlyList<PartitionGroupSummary> Groups,
    IReadOnlyList<UnassignablePartitionClient> UnassignableSample,
    IReadOnlyList<string> Warnings);
