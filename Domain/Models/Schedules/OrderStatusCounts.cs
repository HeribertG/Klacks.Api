// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// How many real-plan orders of a selection are unsealed and sealed - e.g. the orders without any group, or the orders
/// whose customer lies in a place - so a group-scoped answer can say what it does not count.
/// </summary>
/// <param name="OpenOrders">Unsealed orders (OriginalOrder)</param>
/// <param name="SealedOrders">Sealed orders (SealedOrder)</param>
public sealed record OrderStatusCounts(int OpenOrders, int SealedOrders)
{
    public int Total => OpenOrders + SealedOrders;
}
