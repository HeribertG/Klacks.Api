// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Which open orders a seal_open_orders run matched and how it classified them, in full (the result samples are capped):
/// the orders that pass every sealing requirement and the ones that are blocked. A preview carries the selection the user
/// is shown; the confirmed apply carries it as SealOpenOrdersCommand.ExpectedSelection, so the handler seals only when the
/// orders still classify exactly like that and never touches an order that was not part of it.
/// </summary>
/// <param name="SealableIds">Ids of the matched orders that pass every sealing requirement.</param>
/// <param name="BlockedIds">Ids of the matched orders that are blocked by missing or invalid fields.</param>

namespace Klacks.Api.Application.DTOs.Orders;

public sealed record OpenOrderSealingSelection(IReadOnlySet<Guid> SealableIds, IReadOnlySet<Guid> BlockedIds)
{
    public static OpenOrderSealingSelection Empty { get; } = new(new HashSet<Guid>(), new HashSet<Guid>());

    public static OpenOrderSealingSelection Of(IEnumerable<Guid> sealableIds, IEnumerable<Guid> blockedIds) =>
        new(sealableIds.ToHashSet(), blockedIds.ToHashSet());

    public bool Contains(Guid orderId) => SealableIds.Contains(orderId) || BlockedIds.Contains(orderId);

    public bool HasSameEntriesAs(OpenOrderSealingSelection other) =>
        SealableIds.SetEquals(other.SealableIds) && BlockedIds.SetEquals(other.BlockedIds);
}
