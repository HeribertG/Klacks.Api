// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Associations;

/// <summary>
/// Removes the link of one client to one group. A group the caller cannot see is answered like a missing link.
/// </summary>
/// <param name="request">Client and group of the link to remove</param>
public class RemoveGroupItemByClientAndGroupCommandHandler : IRequestHandler<RemoveGroupItemByClientAndGroupCommand, bool>
{
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public RemoveGroupItemByClientAndGroupCommandHandler(
        IGroupItemRepository groupItemRepository,
        IUnitOfWork unitOfWork,
        IGroupVisibilityGuard groupVisibilityGuard)
    {
        _groupItemRepository = groupItemRepository;
        _unitOfWork = unitOfWork;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public async Task<bool> Handle(RemoveGroupItemByClientAndGroupCommand request, CancellationToken cancellationToken)
    {
        if (!await _groupVisibilityGuard.IsGroupVisibleAsync(request.GroupId, cancellationToken))
        {
            return false;
        }

        var groupItem = await _groupItemRepository.GetByClientAndGroup(request.ClientId, request.GroupId);
        if (groupItem == null) return false;

        _groupItemRepository.Remove(groupItem);
        await _unitOfWork.CompleteAsync();
        return true;
    }
}
