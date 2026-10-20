// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Nested-set test used by the diagnosis skills: whether a group lies inside the subtree of another group (the group
/// itself included), the way the schedule of a group also lists the members of its subgroups.
/// </summary>

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Skills;

internal static class GroupSubtree
{
    public static bool Contains(Group root, Group candidate) =>
        candidate.Id == root.Id
        || (candidate.Root == root.Root && candidate.Lft > root.Lft && candidate.Rgt < root.Rgt);
}
