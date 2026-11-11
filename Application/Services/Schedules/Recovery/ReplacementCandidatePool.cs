// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Services.Schedules.Recovery;

/// <summary>
/// The people who can stand in for a group: its own members and the borrowable ones from the rest of its root.
/// </summary>
/// <param name="RootGroupId">Root of the group the pool was resolved for</param>
/// <param name="InGroup">Members of the group and its subgroups</param>
/// <param name="CrossGroup">Everybody else under the root, ordered by id</param>
public sealed record ReplacementCandidatePool(
    Guid RootGroupId,
    IReadOnlyList<Client> InGroup,
    IReadOnlyList<Client> CrossGroup);
