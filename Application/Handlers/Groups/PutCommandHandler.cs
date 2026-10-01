// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Handlers.Groups;

/// <summary>
/// Updates a group's properties, its parent and its client membership (rebuilt from the incoming items).
/// A group-restricted caller may only update a visible group (a hidden or missing one is answered with the
/// same not-found error), may only move it under a visible parent (detaching it to a root stays allowed), and may not
/// add clients they cannot see; removing members stays allowed. All checks run before the self-committing
/// repository write.
/// </summary>
/// <param name="groupRepository">Self-committing group store; also supplies the stored members and parent</param>
/// <param name="groupVisibilityGuard">Decides whether the caller may write the group and its new parent</param>
/// <param name="clientVisibilityGuard">Decides whether the caller may see the added members</param>
public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<GroupResource>, GroupResource?>
{
    private const string GroupNotFoundMessage = "Group with ID {0} not found.";
    private const string NewParentNotFoundMessage = "New parent group with ID {0} not found";

    private readonly IGroupRepository _groupRepository;
    private readonly GroupMapper _groupMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;

    public PutCommandHandler(
        IGroupRepository groupRepository,
        GroupMapper groupMapper,
        IGroupVisibilityGuard groupVisibilityGuard,
        IClientVisibilityGuard clientVisibilityGuard,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _groupRepository = groupRepository;
        _groupMapper = groupMapper;
        _groupVisibilityGuard = groupVisibilityGuard;
        _clientVisibilityGuard = clientVisibilityGuard;
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupResource?> Handle(PutCommand<GroupResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            await EnsureCallerMayUpdateAsync(request.Resource, cancellationToken);

            var group = _groupMapper.ToGroupEntity(request.Resource);
            var updatedGroup = await _groupRepository.Put(group);
            if (updatedGroup == null)
            {
                return null;
            }
            var result = _groupMapper.ToGroupResource(updatedGroup);

            await _unitOfWork.CompleteAsync();

            return result;
        },
        "updating",
        new { });
    }

    private async Task EnsureCallerMayUpdateAsync(GroupResource resource, CancellationToken cancellationToken)
    {
        if (!await _groupVisibilityGuard.IsGroupVisibleAsync(resource.Id, cancellationToken))
        {
            throw GroupNotFound(resource.Id);
        }

        var stored = await _groupRepository.Get(resource.Id) ?? throw GroupNotFound(resource.Id);

        await EnsureParentChangeAllowedAsync(stored, resource.Parent, cancellationToken);

        var storedClientIds = (stored.GroupItems ?? [])
            .Where(item => item.ClientId.HasValue)
            .Select(item => item.ClientId!.Value)
            .ToHashSet();
        var addedClientIds = (resource.GroupItems ?? [])
            .Where(item => item.ClientId.HasValue)
            .Select(item => item.ClientId!.Value)
            .Where(clientId => !storedClientIds.Contains(clientId))
            .ToList();

        await GroupMemberVisibilityCheck.EnsureAddedClientsVisibleAsync(
            _clientVisibilityGuard, addedClientIds, cancellationToken);
    }

    private async Task EnsureParentChangeAllowedAsync(Group stored, Guid? newParent, CancellationToken cancellationToken)
    {
        if (stored.Parent == newParent)
        {
            return;
        }

        if (newParent.HasValue
            && !await _groupVisibilityGuard.IsGroupVisibleAsync(newParent.Value, cancellationToken))
        {
            throw new KeyNotFoundException(string.Format(NewParentNotFoundMessage, newParent.Value));
        }
    }

    private static KeyNotFoundException GroupNotFound(Guid groupId)
    {
        return new KeyNotFoundException(string.Format(GroupNotFoundMessage, groupId));
    }
}
