// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Orders;
using Klacks.Api.Application.DTOs.Orders;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Orders;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Orders;

/// <summary>
/// Handler for <see cref="AssignOrdersToGroupsCommand"/>. Delegates the placement decision to the pure
/// <see cref="OrderGroupPlanner"/>, then with Apply=false only returns the plan; with Apply=true creates
/// one group link per planned order in a single transaction, commits once and re-reads the links to
/// confirm the write, rolling everything back when the count does not match.
/// </summary>
/// <param name="shiftRepository">Loads the open orders with their customer, its addresses and their group links.</param>
/// <param name="groupRepository">Provides the groups the placement is matched against.</param>
/// <param name="groupItemRepository">Adds the new order-to-group links and confirms them after the commit.</param>
/// <param name="unitOfWork">Commits and verifies the new links in a single transaction.</param>
/// <param name="companyClock">Supplies the company-local date used when no explicit ValidFrom is given.</param>
public sealed class AssignOrdersToGroupsCommandHandler
    : IRequestHandler<AssignOrdersToGroupsCommand, AssignOrdersToGroupsResult>
{
    private const string SkillName = "assign_orders_to_groups";
    private const int MaxSample = 20;

    private readonly IShiftRepository _shiftRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICompanyClock _companyClock;

    public AssignOrdersToGroupsCommandHandler(
        IShiftRepository shiftRepository,
        IGroupRepository groupRepository,
        IGroupItemRepository groupItemRepository,
        IUnitOfWork unitOfWork,
        ICompanyClock companyClock)
    {
        _shiftRepository = shiftRepository;
        _groupRepository = groupRepository;
        _groupItemRepository = groupItemRepository;
        _unitOfWork = unitOfWork;
        _companyClock = companyClock;
    }

    public async Task<AssignOrdersToGroupsResult> Handle(
        AssignOrdersToGroupsCommand request, CancellationToken cancellationToken)
    {
        var orders = await _shiftRepository.GetOpenOrdersAsync(
            new OpenOrderFilter(
                request.SourceSystemId,
                request.FromDate,
                request.UntilDate,
                request.CustomerName,
                GroupId: null,
                request.MaxCount),
            cancellationToken);

        var groups = (await _groupRepository.List()).ToList();
        var plan = OrderGroupPlanner.Plan(orders, groups);

        if (!request.Apply || plan.Assignments.Count == 0)
        {
            return BuildResult(plan, applied: request.Apply, verifiedCount: 0);
        }

        var validFrom = request.ValidFrom ?? await _companyClock.GetTodayAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var verified = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var newItems = plan.Assignments
                .Select(assignment => new GroupItem
                {
                    Id = Guid.NewGuid(),
                    ShiftId = assignment.OrderId,
                    GroupId = assignment.GroupId,
                    ValidFrom = validFrom,
                    CreateTime = now,
                    CurrentUserCreated = request.UserName
                })
                .ToList();

            foreach (var item in newItems)
            {
                await _groupItemRepository.Add(item);
            }

            await _unitOfWork.CompleteAsync();

            var confirmed = await _groupItemRepository.CountExistingByIds(
                newItems.Select(i => i.Id).ToList(), cancellationToken);
            if (confirmed != newItems.Count)
            {
                throw new SkillVerificationException(
                    SkillName,
                    $"Database verification failed: expected {newItems.Count} new order-to-group link(s) but only " +
                    $"{confirmed} were confirmed — the changes were rolled back.");
            }

            return confirmed;
        });

        return BuildResult(plan, applied: true, verifiedCount: verified);
    }

    private static AssignOrdersToGroupsResult BuildResult(OrderGroupPlan plan, bool applied, int verifiedCount)
    {
        var targets = plan.Assignments
            .GroupBy(a => (a.GroupId, a.GroupName))
            .Select(byGroup => new OrderGroupTargetSummary(byGroup.Key.GroupName, byGroup.Key.GroupId, byGroup.Count()))
            .OrderByDescending(t => t.OrderCount)
            .ThenBy(t => t.GroupName, StringComparer.Ordinal)
            .ToList();

        return new AssignOrdersToGroupsResult(
            Applied: applied,
            TotalOrders: plan.TotalOrders,
            SkippedAlreadyGroupedCount: plan.SkippedAlreadyGroupedCount,
            AssignedCount: plan.Assignments.Count,
            VerifiedCount: verifiedCount,
            UnassignableCount: plan.Unassignable.Count,
            Targets: targets,
            AssignmentSample: plan.Assignments.Take(MaxSample).ToList(),
            UnassignableSample: plan.Unassignable.Take(MaxSample).ToList());
    }
}
