// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Orders;

/// <summary>
/// Read-only result of OrderGroupPlanner: which open order would be linked to which group, which ones
/// already hold a membership and which ones no group could be derived for. Nothing here is persisted.
/// </summary>
/// <param name="TotalOrders">Number of open orders the planner was handed.</param>
/// <param name="SkippedAlreadyGroupedCount">Orders left untouched because they already hold an active group link.</param>
/// <param name="Assignments">The planned order-to-group placements.</param>
/// <param name="Unassignable">Orders no group could be derived for, with their reason.</param>
public sealed record OrderGroupPlan(
    int TotalOrders,
    int SkippedAlreadyGroupedCount,
    IReadOnlyList<OrderGroupAssignment> Assignments,
    IReadOnlyList<UnassignableOrder> Unassignable);
