// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Groups;

/// <summary>
/// One qualification group in a partition_clients_by_qualification preview or apply result.
/// </summary>
/// <param name="Name">Group name (the qualification name in the installation language).</param>
/// <param name="Existed">True when the group already existed under the parent and is reused.</param>
/// <param name="GroupId">Id of the group; set for a reused group in preview and for every group after apply.</param>
/// <param name="MemberCount">Considered clients holding the qualification today.</param>
/// <param name="NewMemberCount">Members that get a new membership (the others already are current members).</param>
public sealed record QualificationGroupSummary(
    string Name,
    bool Existed,
    Guid? GroupId,
    int MemberCount,
    int NewMemberCount);
