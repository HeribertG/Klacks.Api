// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Orders;

/// <summary>
/// Outcome of seal_open_orders: how many of the matched orders are sealable, how many were sealed, how
/// many are blocked by missing fields and how many failed while being sealed. With Applied=false nothing
/// was written and SealedCount is 0.
/// </summary>
/// <param name="Applied">False for a preview, true when the batch was actually sealed.</param>
/// <param name="TotalOrders">Number of open orders that matched the filter.</param>
/// <param name="SealableCount">Orders that pass every sealing requirement.</param>
/// <param name="SealedCount">Orders that were sealed and confirmed in the database; 0 on a preview.</param>
/// <param name="BlockedCount">Orders that cannot be sealed because fields are missing or invalid.</param>
/// <param name="FailedCount">Orders whose sealing was attempted and failed; 0 on a preview.</param>
/// <param name="BlockedOnlyByMissingGroupCount">Blocked orders whose sole missing requirement is a group, so the group assignment alone would unblock them.</param>
/// <param name="AutoAssignedCount">Group links created by the preceding group assignment; 0 when it was not requested.</param>
/// <param name="AutoAssignRequested">True when the caller asked for the group assignment to run first.</param>
/// <param name="SealedSample">A capped sample of the sealed orders and their plannable shifts.</param>
/// <param name="BlockedSample">A capped sample of the blocked orders and what they are missing.</param>
/// <param name="Failures">A capped list of the failed orders and their error message.</param>
public sealed record SealOpenOrdersResult(
    bool Applied,
    int TotalOrders,
    int SealableCount,
    int SealedCount,
    int BlockedCount,
    int FailedCount,
    int BlockedOnlyByMissingGroupCount,
    int AutoAssignedCount,
    bool AutoAssignRequested,
    IReadOnlyList<SealedOrderInfo> SealedSample,
    IReadOnlyList<BlockedOrder> BlockedSample,
    IReadOnlyList<FailedOrder> Failures);
