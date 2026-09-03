// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Orders;

/// <summary>
/// Outcome of assign_orders_to_groups: the per-group counts, a sample of the placements and the orders
/// that stayed unassigned. With Applied=false nothing was written and every count describes the plan.
/// </summary>
/// <param name="Applied">False for a preview, true when the group links were persisted.</param>
/// <param name="TotalOrders">Number of open orders that matched the filter.</param>
/// <param name="SkippedAlreadyGroupedCount">Orders skipped because they already hold an active group link.</param>
/// <param name="AssignedCount">Orders that were, or would be, linked to a group.</param>
/// <param name="VerifiedCount">Links re-read from the database after the write; 0 on a preview.</param>
/// <param name="UnassignableCount">Orders no group could be derived for.</param>
/// <param name="Targets">Per-group order counts.</param>
/// <param name="AssignmentSample">A capped sample of the individual placements, with their match reason.</param>
/// <param name="UnassignableSample">A capped sample of the unassigned orders, with their reason.</param>
public sealed record AssignOrdersToGroupsResult(
    bool Applied,
    int TotalOrders,
    int SkippedAlreadyGroupedCount,
    int AssignedCount,
    int VerifiedCount,
    int UnassignableCount,
    IReadOnlyList<OrderGroupTargetSummary> Targets,
    IReadOnlyList<OrderGroupAssignment> AssignmentSample,
    IReadOnlyList<UnassignableOrder> UnassignableSample);
