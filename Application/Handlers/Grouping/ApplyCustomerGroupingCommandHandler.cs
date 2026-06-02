// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for <see cref="ApplyCustomerGroupingCommand"/>. Recomputes the proposal via the shared
/// <see cref="ICustomerGroupingPlanner"/> (so the apply always matches a fresh dry run) and persists it
/// directly on the group_item table: it soft-deletes the replaced location memberships and adds the
/// target membership, all committed in a single save. Re-running it is a no-op once everything sits in
/// its nearest group.
/// </summary>
/// <param name="planner">Recomputes the assignment proposal to apply.</param>
/// <param name="groupItemRepository">Loads, removes and adds the individual group memberships.</param>
/// <param name="unitOfWork">Commits all membership changes in a single save.</param>

using Klacks.Api.Application.Commands.Grouping;
using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Services.Grouping;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Grouping;

public sealed class ApplyCustomerGroupingCommandHandler
    : IRequestHandler<ApplyCustomerGroupingCommand, CustomerGroupingApplyResult>
{
    private readonly ICustomerGroupingPlanner _planner;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ApplyCustomerGroupingCommandHandler(
        ICustomerGroupingPlanner planner,
        IGroupItemRepository groupItemRepository,
        IUnitOfWork unitOfWork)
    {
        _planner = planner;
        _groupItemRepository = groupItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerGroupingApplyResult> Handle(
        ApplyCustomerGroupingCommand request, CancellationToken cancellationToken)
    {
        var proposal = await _planner.BuildProposalAsync(cancellationToken);
        var movedCount = 0;

        foreach (var assignment in proposal.Assignments)
        {
            var changed = false;

            foreach (var retireGroupId in assignment.RetireGroupIds)
            {
                var membership = await _groupItemRepository.GetByClientAndGroup(assignment.ClientId, retireGroupId);
                if (membership != null)
                {
                    _groupItemRepository.Remove(membership);
                    changed = true;
                }
            }

            var existingTarget = await _groupItemRepository.GetByClientAndGroup(
                assignment.ClientId, assignment.TargetGroupId);
            if (existingTarget == null)
            {
                await _groupItemRepository.Add(new GroupItem
                {
                    Id = Guid.NewGuid(),
                    ClientId = assignment.ClientId,
                    GroupId = assignment.TargetGroupId,
                    ValidFrom = DateTime.UtcNow,
                    CreateTime = DateTime.UtcNow
                });
                changed = true;
            }

            if (changed)
            {
                movedCount++;
            }
        }

        if (movedCount > 0)
        {
            await _unitOfWork.CompleteAsync();
        }

        return new CustomerGroupingApplyResult(movedCount, proposal.Unassigned.Count);
    }
}
