// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The open orders a seal_open_orders run may touch, resolved once from the caller and carried on
/// SealOpenOrdersCommand, so the preview, the inline apply and the deferred background job all filter with the
/// very same scope. Full access covers every open order, including orders without any group. A group scope
/// covers only orders linked to at least one visible group (the same "any visible group" rule seal_shift's
/// ShiftVisibilityGuard applies to a single order) and never an order without a group, which stays admin-only.
/// </summary>
/// <param name="HasFullAccess">True only for an administrator whose group scope is unrestricted.</param>
/// <param name="VisibleGroupIds">Flat ids of every group the caller can see, whole subtrees included; ignored with full access.</param>

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.DTOs.Orders;

public sealed record OpenOrderSealingScope(bool HasFullAccess, IReadOnlySet<Guid> VisibleGroupIds)
{
    public static OpenOrderSealingScope FullAccess() => new(true, new HashSet<Guid>());

    public static OpenOrderSealingScope ForGroups(IEnumerable<Guid> visibleGroupIds) =>
        new(false, visibleGroupIds.ToHashSet());

    public bool Includes(Shift order) =>
        HasFullAccess
        || order.GroupItems.Any(gi => !gi.IsDeleted && VisibleGroupIds.Contains(gi.GroupId));
}
