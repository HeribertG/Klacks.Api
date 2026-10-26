// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Orders;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Derives the group one open order without a group is placed in when seal_shift seals it, with exactly the rule
/// assign_orders_to_groups places its batch by (OrderGroupPlanner), so the two never disagree.
/// </summary>
public interface IOrderGroupPlacementResolver
{
    /// <param name="orderId">Id of the open order (status OriginalOrder).</param>
    /// <param name="cancellationToken">Token cancelling the reads.</param>
    Task<OrderGroupPlacement> ResolveAsync(Guid orderId, CancellationToken cancellationToken = default);
}
