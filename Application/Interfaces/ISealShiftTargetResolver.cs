// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Orders;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// The one place seal_shift's preview and its confirmed replay work out what would be sealed, so the two never diverge:
/// the order, and for an order without a group the group derived from the customer's address.
/// </summary>
public interface ISealShiftTargetResolver
{
    /// <summary>
    /// Resolves the order a seal would run on, deriving a missing group from the customer's address, or returns the refusal reason.
    /// </summary>
    /// <param name="orderId">Id of the order (status OriginalOrder) to seal.</param>
    /// <param name="cancellationToken">Token cancelling the reads.</param>
    Task<SealShiftTarget> ResolveAsync(Guid orderId, CancellationToken cancellationToken = default);
}
