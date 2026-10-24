// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
    /// Loads the order and decides, without writing, whether SealAsync would seal it right now; a refusal carries the
    /// very outcome SealAsync returns for it. Used to preview a single sealing before it is confirmed.
    /// </summary>
    /// <param name="orderId">Id of the order (status OriginalOrder) to check.</param>
    /// <param name="cancellationToken">Token cancelling the read of the order.</param>
    Task<OrderSealingCheck> CheckAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// CheckAsync for seal_shift with the group it would attach: the group satisfies the group requirement, and an order
    /// that has a group of its own is refused (it would otherwise get a second one).
    /// </summary>
    /// <param name="orderId">Id of the order (status OriginalOrder) to check.</param>
    /// <param name="groupToAttach">The group derived from the customer's address; null checks exactly like CheckAsync.</param>
    /// <param name="cancellationToken">Token cancelling the read of the order.</param>
    Task<OrderSealingCheck> CheckAsync(Guid orderId, Guid? groupToAttach, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the order, refuses it when its status or its fields do not allow sealing, and otherwise
    /// performs the transition inside its own transaction, re-reading both the sealed order and the
    /// created plannable shift before the commit.
    /// </summary>
    /// <param name="orderId">Id of the order (status OriginalOrder) to seal.</param>
    /// <param name="cancellationToken">Token cancelling the read of the order.</param>
    Task<OrderSealingOutcome> SealAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// SealAsync for seal_shift with the group to attach: the group is added to the order (valid from its start) in the
    /// same transaction, the clone carries it to the plannable shift, and the link is verified after the commit.
    /// </summary>
    /// <param name="orderId">Id of the order (status OriginalOrder) to seal.</param>
    /// <param name="groupToAttach">The group derived from the customer's address; null seals exactly like SealAsync.</param>
    /// <param name="cancellationToken">Token cancelling the read of the order.</param>
    Task<OrderSealingOutcome> SealAsync(Guid orderId, Guid? groupToAttach, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the sealing transition of an order CheckAsync returned as sealable inside the caller's open transaction,
    /// without opening one of its own, and re-reads the sealed order and the created plannable shift. Returns the id of
    /// the plannable shift; throws SkillVerificationException when the write or its verification fails, so the caller's
    /// transaction rolls back.
    /// </summary>
    /// <param name="sealableOrder">The order as CheckAsync loaded it; its status is set to SealedOrder.</param>
    /// <param name="cancellationToken">Token cancelling the verification reads.</param>
    /// <param name="groupToAttach">Group to add to the order (valid from its start) inside the same transaction; null adds none.</param>
    Task<Guid> SealWithinTransactionAsync(Shift sealableOrder, CancellationToken cancellationToken = default, Guid? groupToAttach = null);
}
