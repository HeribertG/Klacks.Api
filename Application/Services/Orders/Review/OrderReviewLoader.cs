// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads the order review. The scope follows seal_open_orders: an administrator with unrestricted group scope reaches
/// every open order, including orders without a group (and only he gets group proposals for them); every other caller
/// reaches the open orders linked to a group of his scope and is refused without one. The items are the analysed open
/// orders (OrderReviewAnalyzer) with what still blocks sealing them, the group proposals of OrderGroupPlanner, the
/// scheduling policies of each executing group's members at the company's today and all qualifications.
/// </summary>
/// <param name="shiftRepository">Loads the open orders with their customer and group links.</param>
/// <param name="groupRepository">Lists the groups.</param>
/// <param name="groupScopeGuard">Tells whether the caller's group scope is unrestricted and which groups it covers.</param>
/// <param name="orderSealingService">Names the fields that still block sealing an order.</param>
/// <param name="memberPolicyReader">Reads the scheduling policies of a group's members.</param>
/// <param name="qualificationRepository">Lists the qualifications the name heuristic matches against.</param>
/// <param name="companyClock">The company's today.</param>

using Klacks.Api.Application.DTOs.Orders;
using Klacks.Api.Application.DTOs.Orders.Review;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.OrderReview;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Orders.Review;

public sealed class OrderReviewLoader : IOrderReviewLoader
{
    private const string NoGroupScopeError =
        "No group is assigned to your group scope, so there is no open order you may review. " +
        "Ask an administrator to assign you a group scope.";

    private readonly IShiftRepository _shiftRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly IOrderSealingService _orderSealingService;
    private readonly IGroupMemberPolicyReader _memberPolicyReader;
    private readonly IQualificationRepository _qualificationRepository;
    private readonly ICompanyClock _companyClock;

    public OrderReviewLoader(
        IShiftRepository shiftRepository,
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        IOrderSealingService orderSealingService,
        IGroupMemberPolicyReader memberPolicyReader,
        IQualificationRepository qualificationRepository,
        ICompanyClock companyClock)
    {
        _shiftRepository = shiftRepository;
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _orderSealingService = orderSealingService;
        _memberPolicyReader = memberPolicyReader;
        _qualificationRepository = qualificationRepository;
        _companyClock = companyClock;
    }

    public async Task<OrderReviewScope> ResolveScopeAsync(
        SkillExecutionContext context, CancellationToken cancellationToken = default)
    {
        var access = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        var hasFullAccess = access.IsUnrestricted && context.UserPermissions.Contains(Roles.Admin);
        var allGroups = await _groupRepository.List();
        var reachable = hasFullAccess ? allGroups : allGroups.Where(access.IsInScope).ToList();
        if (!hasFullAccess && reachable.Count == 0)
        {
            return OrderReviewScope.Refuse(NoGroupScopeError);
        }

        var sealingScope = hasFullAccess
            ? OpenOrderSealingScope.FullAccess()
            : OpenOrderSealingScope.ForGroups(reachable.Select(group => group.Id));

        return new OrderReviewScope(null, hasFullAccess, reachable, allGroups, sealingScope);
    }

    public async Task<List<OrderReviewDraftItem>> LoadItemsAsync(
        SkillExecutionContext context,
        OrderReviewScope scope,
        string? customerName,
        CancellationToken cancellationToken = default)
    {
        var orders = (await _shiftRepository.GetOpenOrdersAsync(
                new OpenOrderFilter(null, null, null, customerName, null, null), cancellationToken))
            .Where(scope.SealingScope.Includes)
            .ToList();
        if (orders.Count == 0)
        {
            return [];
        }

        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var groupPlan = scope.HasFullAccess
            ? OrderGroupPlanner.Plan(orders, scope.AllGroups, today)
            : new OrderGroupPlan(orders.Count, orders.Count, [], []);
        var proposals = groupPlan.Assignments.ToDictionary(assignment => assignment.OrderId);

        var policies = await LoadMemberPoliciesAsync(
            ExecutingGroupIds(orders, proposals), DateOnly.FromDateTime(today), cancellationToken);

        var input = new OrderReviewAnalysisInput(
            orders,
            orders.ToDictionary(order => order.Id, order => _orderSealingService.CollectMissingRequirements(order)),
            proposals,
            groupPlan.Unassignable.ToDictionary(unassignable => unassignable.OrderId, unassignable => unassignable.Reason),
            scope.AllGroups.ToDictionary(group => group.Id, group => group.Name),
            policies,
            await _qualificationRepository.GetAllAsync(cancellationToken),
            context.UserLanguage);

        return OrderReviewAnalyzer.Analyze(input);
    }

    private static HashSet<Guid> ExecutingGroupIds(
        IEnumerable<Shift> orders, IReadOnlyDictionary<Guid, OrderGroupAssignment> proposals) =>
        orders
            .Select(order => order.GroupItems.FirstOrDefault(link => !link.IsDeleted && link.AnalyseToken == null)?.GroupId
                ?? (proposals.TryGetValue(order.Id, out var proposal) ? proposal.GroupId : (Guid?)null))
            .Where(groupId => groupId.HasValue)
            .Select(groupId => groupId!.Value)
            .ToHashSet();

    private async Task<Dictionary<Guid, IReadOnlyList<SchedulingPolicy>>> LoadMemberPoliciesAsync(
        IEnumerable<Guid> groupIds, DateOnly date, CancellationToken cancellationToken)
    {
        var policies = new Dictionary<Guid, IReadOnlyList<SchedulingPolicy>>();
        foreach (var groupId in groupIds)
        {
            policies[groupId] = await _memberPolicyReader.GetMemberPoliciesAsync(groupId, date, cancellationToken);
        }

        return policies;
    }
}
