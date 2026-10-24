// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// How many real-plan orders are linked to no group at all, so a group-scoped answer can say what it does not count.
/// </summary>
/// <param name="OpenOrders">Unsealed orders (OriginalOrder) without a live group link</param>
/// <param name="SealedOrders">Sealed orders (SealedOrder) without a live group link</param>
public sealed record UngroupedOrderCounts(int OpenOrders, int SealedOrders)
{
    public int Total => OpenOrders + SealedOrders;
}
