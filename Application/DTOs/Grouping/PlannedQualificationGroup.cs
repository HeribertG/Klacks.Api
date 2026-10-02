// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

/// <summary>
/// One qualification group QualificationGroupPlanner would create or reuse under the qualification parent.
/// </summary>
/// <param name="Name">Group name: the qualification name in the installation language.</param>
/// <param name="QualificationIds">Qualifications behind the group; more than one when several qualifications resolve to the same name.</param>
/// <param name="Existed">True when a group with this name already sits under the parent and is reused.</param>
/// <param name="ExistingGroupId">Id of the reused group; null for a new group.</param>
/// <param name="MemberClientIds">Every considered client holding one of the qualifications today.</param>
/// <param name="NewMemberClientIds">Members that are not yet current members of the reused group (all members for a new group); only these get a new membership.</param>
public sealed record PlannedQualificationGroup(
    string Name,
    IReadOnlyList<Guid> QualificationIds,
    bool Existed,
    Guid? ExistingGroupId,
    IReadOnlyList<Guid> MemberClientIds,
    IReadOnlyList<Guid> NewMemberClientIds);
