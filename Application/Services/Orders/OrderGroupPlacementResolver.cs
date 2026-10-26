// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Derives the group of one open order for seal_shift. It loads the order the way assign_orders_to_groups loads its batch
/// (GetOpenOrdersAsync, filtered by the order id, so the customer's addresses and memberships come along) and plans it with
/// OrderGroupPlanner against all groups - the same rule, not a copy of it, so a later change of the planner's address
/// precedence reaches both. A derived group the caller may not see is answered like no group at all and is not named.
/// It only reads.
/// </summary>
/// <param name="shiftRepository">Loads the open order with its customer, the customer's addresses and memberships</param>
/// <param name="groupRepository">Provides the groups the placement is matched against</param>
/// <param name="companyClock">Supplies the company-local date the customer's memberships are judged at</param>
/// <param name="groupVisibilityGuard">Hides a derived group the caller may not see</param>

using Klacks.Api.Application.DTOs.Orders;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Application.Services.Orders;

public sealed class OrderGroupPlacementResolver : IOrderGroupPlacementResolver
{
    private const int SingleOrder = 1;

    private const string ReasonNotAnOpenOrder = "the order is not an open order";
    private const string ReasonAlreadyGrouped = "the order already has a group";
    private const string ReasonNoVisibleGroup = "no group you may see matches the customer's address";
    private const string ReasonUnknown = "no group could be derived";

    private readonly IShiftRepository _shiftRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly ICompanyClock _companyClock;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public OrderGroupPlacementResolver(
        IShiftRepository shiftRepository,
        IGroupRepository groupRepository,
        ICompanyClock companyClock,
        IGroupVisibilityGuard groupVisibilityGuard)
    {
        _shiftRepository = shiftRepository;
        _groupRepository = groupRepository;
        _companyClock = companyClock;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public async Task<OrderGroupPlacement> ResolveAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var orders = await _shiftRepository.GetOpenOrdersAsync(
            new OpenOrderFilter(
                SourceSystemId: null, FromDate: null, UntilDate: null, CustomerName: null, GroupId: null,
                MaxCount: SingleOrder, OrderId: orderId),
            cancellationToken);
        if (orders.Count == 0)
        {
            return new OrderGroupPlacement(null, ReasonNotAnOpenOrder);
        }

        var groups = (await _groupRepository.List()).ToList();
        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var plan = OrderGroupPlanner.Plan(orders, groups, today);

        var assignment = plan.Assignments.FirstOrDefault();
        if (assignment == null)
        {
            var reason = plan.SkippedAlreadyGroupedCount > 0
                ? ReasonAlreadyGrouped
                : plan.Unassignable.FirstOrDefault()?.Reason ?? ReasonUnknown;
            return new OrderGroupPlacement(null, reason);
        }

        return await _groupVisibilityGuard.IsGroupVisibleAsync(assignment.GroupId, cancellationToken)
            ? new OrderGroupPlacement(assignment, null)
            : new OrderGroupPlacement(null, ReasonNoVisibleGroup);
    }
}
