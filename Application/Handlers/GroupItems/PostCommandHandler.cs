// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.GroupItems;

/// <summary>
/// Links a client or a shift to a group. A group-restricted caller may only link into a visible group and only
/// a client they can see: a hidden group or client is answered like a missing one, so a supervisor cannot pull
/// a foreign client into their own group and thereby make it visible.
/// </summary>
/// <param name="request">The link to create</param>
public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<GroupItemResource>, GroupItemResource?>
{
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly GroupMapper _groupMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;

    public PostCommandHandler(
        IGroupItemRepository groupItemRepository,
        GroupMapper groupMapper,
        IUnitOfWork unitOfWork,
        IGroupVisibilityGuard groupVisibilityGuard,
        IClientVisibilityGuard clientVisibilityGuard,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _groupItemRepository = groupItemRepository;
        _groupMapper = groupMapper;
        _unitOfWork = unitOfWork;
        _groupVisibilityGuard = groupVisibilityGuard;
        _clientVisibilityGuard = clientVisibilityGuard;
    }

    public async Task<GroupItemResource?> Handle(PostCommand<GroupItemResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (!await _groupVisibilityGuard.IsGroupVisibleAsync(request.Resource.GroupId, cancellationToken))
            {
                throw new KeyNotFoundException(
                    string.Format(GroupItemVisibilityMessages.GroupNotFound, request.Resource.GroupId));
            }

            if (request.Resource.ClientId.HasValue)
            {
                await GroupMemberVisibilityCheck.EnsureAddedClientsVisibleAsync(
                    _clientVisibilityGuard, [request.Resource.ClientId.Value], cancellationToken);
            }

            var groupItem = _groupMapper.ToGroupItemEntity(request.Resource);
            await _groupItemRepository.Add(groupItem);
            await _unitOfWork.CompleteAsync();
            return _groupMapper.ToGroupItemResource(groupItem);
        },
        "creating group item",
        new { });
    }
}
