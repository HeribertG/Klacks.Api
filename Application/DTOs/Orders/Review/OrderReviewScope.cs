// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.DTOs.Orders.Review;

/// <summary>
/// The caller's reach in the order review: full access (an administrator with unrestricted group scope, who also sees
/// orders without any group and may give them a group) or the groups of a group scope; the groups a group name may be
/// resolved against; every group for display names; and the open-order scope the orders are filtered with.
/// </summary>
/// <param name="Refusal">Why the caller cannot review any order; null when the scope is usable</param>
/// <param name="HasFullAccess">True only for an administrator whose group scope is unrestricted</param>
/// <param name="ReachableGroups">Groups a group name may be resolved against</param>
/// <param name="AllGroups">Every group, for the names of groups orders already belong to and the group proposals</param>
/// <param name="SealingScope">The open orders the caller may review</param>
public sealed record OrderReviewScope(
    string? Refusal,
    bool HasFullAccess,
    IReadOnlyList<Group> ReachableGroups,
    IReadOnlyList<Group> AllGroups,
    OpenOrderSealingScope SealingScope)
{
    public static OrderReviewScope Refuse(string refusal) =>
        new(refusal, false, [], [], OpenOrderSealingScope.ForGroups([]));
}
