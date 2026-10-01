// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Groups;

/// <summary>
/// Moves a group (with its subtree) under a new parent. A group-restricted caller may only move a visible
/// group under a visible parent; a hidden node or a hidden new parent is answered exactly like a missing one,
/// before the self-committing repository move runs.
/// </summary>
/// <param name="groupRepository">Self-committing nested-set aware group store</param>
/// <param name="groupVisibilityGuard">Decides whether the caller may write the node and the new parent</param>
public class MoveGroupNodeCommandHandler : IRequestHandler<MoveGroupNodeCommand, GroupResource>
{
    private const string NodeNotFoundMessage = "Group to be moved with ID {0} not found";
    private const string NewParentNotFoundMessage = "New parent group with ID {0} not found";

    private readonly IGroupRepository _groupRepository;
    private readonly GroupMapper _groupMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MoveGroupNodeCommandHandler> _logger;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public MoveGroupNodeCommandHandler(
        IGroupRepository groupRepository,
        GroupMapper groupMapper,
        IGroupVisibilityGuard groupVisibilityGuard,
        IUnitOfWork unitOfWork,
        ILogger<MoveGroupNodeCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _groupMapper = groupMapper;
        _groupVisibilityGuard = groupVisibilityGuard;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GroupResource> Handle(MoveGroupNodeCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            _logger.LogInformation("Move node {NodeId} to new parent {NewParentId}", request.NodeId, request.NewParentId);

            if (!await _groupVisibilityGuard.IsGroupVisibleAsync(request.NodeId, cancellationToken))
            {
                throw new KeyNotFoundException(string.Format(NodeNotFoundMessage, request.NodeId));
            }

            if (!await _groupVisibilityGuard.IsGroupVisibleAsync(request.NewParentId, cancellationToken))
            {
                throw new KeyNotFoundException(string.Format(NewParentNotFoundMessage, request.NewParentId));
            }

            await _groupRepository.MoveNode(request.NodeId, request.NewParentId);

            var movedGroup = await _groupRepository.Get(request.NodeId);
            if (movedGroup == null)
            {
                throw new KeyNotFoundException($"Group with ID {request.NodeId} not found after move");
            }

            var depth = await _groupRepository.GetNodeDepth(request.NodeId);
            var result = _groupMapper.ToGroupResource(movedGroup);
            result.Depth = depth;

            _logger.LogInformation("Node {NodeId} successfully moved to parent {NewParentId}", request.NodeId, request.NewParentId);

            return result;
        });
    }
}