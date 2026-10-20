// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Groups;

namespace Klacks.Api.Application.DTOs.Grouping;

public sealed record GroupingApplyResult(
    Guid? CreatedGroupId,
    int CreatedGroups,
    int AddedShifts,
    int AddedClients,
    int RemovedClients,
    int AlreadyInPlace,
    GroupMembershipStartInfo? MembershipStart = null);
