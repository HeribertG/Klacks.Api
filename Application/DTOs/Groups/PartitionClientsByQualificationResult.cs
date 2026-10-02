// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Grouping;

namespace Klacks.Api.Application.DTOs.Groups;

/// <summary>
/// Result of partitioning clients into qualification groups. With Applied=false it is a dry-run preview; with
/// Applied=true the groups and memberships were persisted and the new memberships were re-read for verification.
/// </summary>
/// <param name="Applied">False for a preview, true when groups and memberships were persisted.</param>
/// <param name="EntityType">The client type(s) considered: a single type name, or "All".</param>
/// <param name="ParentGroupName">Name of the group the qualification groups sit under.</param>
/// <param name="ParentExisted">False when the qualifications root group is (or was) created by this run.</param>
/// <param name="ParentGroupId">Id of the parent group; null in a preview that would create it.</param>
/// <param name="IsScoped">True when only members of the parent group's subtree were considered (mixed form).</param>
/// <param name="MinMembers">Minimum number of holders a qualification needed to get a group.</param>
/// <param name="TotalClients">Clients of the requested types that were loaded.</param>
/// <param name="ConsideredClients">Clients inside the scope (all clients without a scope).</param>
/// <param name="SkippedAlreadyGroupedCount">Considered clients skipped because they already hold a membership and includeAlreadyGrouped was false.</param>
/// <param name="ClientsWithoutQualificationCount">Considered clients holding no qualification valid today.</param>
/// <param name="AssignedCount">New memberships created (Applied only); in a preview the number that would be created.</param>
/// <param name="VerifiedCount">New memberships re-read and confirmed in the database (Applied only).</param>
/// <param name="AlreadyMemberCount">Planned placements that already existed as a current membership.</param>
/// <param name="Groups">Planned or created qualification groups, ordered by name.</param>
/// <param name="SkippedQualifications">Qualifications below the minimum, ordered by name.</param>
/// <param name="Warnings">Non-fatal issues worth surfacing.</param>
/// <param name="UsersKeepingFullVisibilityCount">Only non-zero when this run introduces the very first group of the installation: how many non-admin users keep visibility on the new root group.</param>
/// <param name="RestrictedUsersSeeingRootCount">When an existing qualifications root is reused without a scope group: non-admin users with visibility on that root, who inherit visibility on every new member.</param>
/// <param name="ClientsNewlyVisibleToThemCount">Clients that join the reused root's subtree for the first time and so become visible to those users.</param>
public sealed record PartitionClientsByQualificationResult(
    bool Applied,
    string EntityType,
    string ParentGroupName,
    bool ParentExisted,
    Guid? ParentGroupId,
    bool IsScoped,
    int MinMembers,
    int TotalClients,
    int ConsideredClients,
    int SkippedAlreadyGroupedCount,
    int ClientsWithoutQualificationCount,
    int AssignedCount,
    int VerifiedCount,
    int AlreadyMemberCount,
    IReadOnlyList<QualificationGroupSummary> Groups,
    IReadOnlyList<SkippedQualificationGroup> SkippedQualifications,
    IReadOnlyList<string> Warnings,
    int UsersKeepingFullVisibilityCount,
    int RestrictedUsersSeeingRootCount = 0,
    int ClientsNewlyVisibleToThemCount = 0);
