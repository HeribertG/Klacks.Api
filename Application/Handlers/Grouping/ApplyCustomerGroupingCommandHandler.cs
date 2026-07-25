// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for <see cref="ApplyCustomerGroupingCommand"/>. Recomputes the proposal via the shared
/// <see cref="ICustomerGroupingPlanner"/> (so the apply always matches a fresh dry run) and persists it
/// directly on the group_item table: it soft-deletes the replaced location memberships and adds the
/// target membership, all committed in a single save. The returned ended-membership count counts the
/// retires that actually found a live membership, so it is the applied truth rather than the plan — a
/// membership that vanished between the dry run and the apply is not counted. Re-running it is a no-op
/// once everything sits in its target group.
/// </summary>
/// <param name="planner">Recomputes the assignment proposal to apply.</param>
/// <param name="groupItemRepository">Loads, removes and adds the individual group memberships.</param>
/// <param name="unitOfWork">Commits all membership changes in a single save.</param>
/// <param name="companyClock">Supplies the company-local date used for the new membership ValidFrom when the caller did not request an explicit one.</param>

using Klacks.Api.Application.Commands.Grouping;
using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Services.Grouping;
using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Grouping;

public sealed class ApplyCustomerGroupingCommandHandler
    : IRequestHandler<ApplyCustomerGroupingCommand, CustomerGroupingApplyResult>
{
    private readonly ICustomerGroupingPlanner _planner;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICompanyClock _companyClock;

    public ApplyCustomerGroupingCommandHandler(
        ICustomerGroupingPlanner planner,
        IGroupItemRepository groupItemRepository,
        IUnitOfWork unitOfWork,
        ICompanyClock companyClock)
    {
        _planner = planner;
        _groupItemRepository = groupItemRepository;
        _unitOfWork = unitOfWork;
        _companyClock = companyClock;
    }

    public async Task<CustomerGroupingApplyResult> Handle(
        ApplyCustomerGroupingCommand request, CancellationToken cancellationToken)
    {
        var proposal = await _planner.BuildProposalAsync(request.EntityType, cancellationToken);
        var validFrom = request.ValidFrom ?? await _companyClock.GetTodayAsync(cancellationToken);

        if (proposal.Assignments.Count == 0)
        {
            return new CustomerGroupingApplyResult(0, 0, proposal.Unassigned.Count, 0, validFrom);
        }

        var addedItems = new List<GroupItem>();
        var endedMembershipCount = 0;
        var movedCount = 0;

        movedCount = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var moved = 0;
            foreach (var assignment in proposal.Assignments)
            {
                var changed = false;

                foreach (var retireGroupId in assignment.RetireGroupIds)
                {
                    var membership = await _groupItemRepository.GetByClientAndGroup(assignment.ClientId, retireGroupId);
                    if (membership != null)
                    {
                        _groupItemRepository.Remove(membership);
                        endedMembershipCount++;
                        changed = true;
                    }
                }

                var existingTarget = await _groupItemRepository.GetByClientAndGroup(
                    assignment.ClientId, assignment.TargetGroupId);
                if (existingTarget == null)
                {
                    var item = new GroupItem
                    {
                        Id = Guid.NewGuid(),
                        ClientId = assignment.ClientId,
                        GroupId = assignment.TargetGroupId,
                        ValidFrom = validFrom,
                        CreateTime = DateTime.UtcNow
                    };
                    await _groupItemRepository.Add(item);
                    addedItems.Add(item);
                    changed = true;
                }

                if (changed)
                {
                    moved++;
                }
            }

            await _unitOfWork.CompleteAsync();

            if (addedItems.Count > 0)
            {
                var confirmed = await _groupItemRepository.CountExistingByIds(
                    addedItems.Select(i => i.Id).ToList(), cancellationToken);
                if (confirmed != addedItems.Count)
                {
                    throw new SkillVerificationException(
                        "apply_grouping",
                        $"Database verification failed: expected {addedItems.Count} new nearest-group memberships " +
                        $"but only {confirmed} were confirmed — the changes were rolled back.");
                }
            }

            return moved;
        });

        return new CustomerGroupingApplyResult(
            movedCount, addedItems.Count, proposal.Unassigned.Count, endedMembershipCount, validFrom);
    }
}
