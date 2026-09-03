// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Orders;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// The single sealing path for an order: the one-way OriginalOrder to SealedOrder transition that
/// simultaneously creates the plannable OriginalShift. Both seal_shift (one order) and seal_open_orders
/// (a whole batch) run through it so there is never a second sealing algorithm to keep in sync.
/// </summary>
public interface IOrderSealingService
{
    /// <summary>
    /// Lists the field names that still block sealing this order, without touching the database.
    /// An empty list means the order is sealable. The order must have been loaded with its GroupItems.
    /// </summary>
    /// <param name="order">Order to inspect; its GroupItems collection must be populated.</param>
    IReadOnlyList<string> CollectMissingRequirements(Shift order);

    /// <summary>
    /// Loads the order, refuses it when its status or its fields do not allow sealing, and otherwise
    /// performs the transition inside its own transaction, re-reading both the sealed order and the
    /// created plannable shift before the commit.
    /// </summary>
    /// <param name="orderId">Id of the order (status OriginalOrder) to seal.</param>
    /// <param name="cancellationToken">Token cancelling the read of the order.</param>
    Task<OrderSealingOutcome> SealAsync(Guid orderId, CancellationToken cancellationToken = default);
}
