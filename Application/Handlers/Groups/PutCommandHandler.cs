// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Handlers.Groups;

/// <summary>
/// Updates a group's properties, its parent and its client membership (rebuilt from the incoming items).
/// A group-restricted caller may only update a visible group (a hidden or missing one is answered with the
/// same not-found error) and may not add clients they cannot see; removing members stays allowed. A caller without
/// the Admin role (the supervisor, owner decision 2026-11-08) edits only name, description, validity and members of a
/// group whose parent is visible: changing the parent (a hidden move), editing a root group, or changing payment
/// interval or holiday calendar is forbidden (403). The Admin keeps every change, including reparenting. All checks
/// run before the self-committing repository write. Skills reach this handler through the REST endpoint with the
/// caller's token.
/// </summary>
/// <param name="groupRepository">Self-committing group store; also supplies the stored members, parent and settings</param>
/// <param name="groupVisibilityGuard">Decides whether the caller may write the group and its parent</param>
/// <param name="clientVisibilityGuard">Decides whether the caller may see the added members</param>
/// <param name="userService">Tells whether the caller holds the Admin role (role claim, like the endpoint gate)</param>
public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<GroupResource>, GroupResource?>
{
    private const string GroupNotFoundMessage = "Group with ID {0} not found.";
    private const string SubgroupEditOnlyMessage =
        "Only an administrator may move a group, edit a root group or change payment interval or holiday calendar; a supervisor edits name, description, validity and members of a subgroup.";

    private readonly IGroupRepository _groupRepository;
    private readonly GroupMapper _groupMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IUserService _userService;

    public PutCommandHandler(
        IGroupRepository groupRepository,
        GroupMapper groupMapper,
        IGroupVisibilityGuard groupVisibilityGuard,
        IClientVisibilityGuard clientVisibilityGuard,
        IUserService userService,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _groupRepository = groupRepository;
        _groupMapper = groupMapper;
        _groupVisibilityGuard = groupVisibilityGuard;
        _clientVisibilityGuard = clientVisibilityGuard;
        _userService = userService;
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

        if (!_userService.GetRights().Contains(Roles.Admin))
        {
            await EnsureSubgroupEditOnlyAsync(stored, resource, cancellationToken);
        }

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

    private async Task EnsureSubgroupEditOnlyAsync(Group stored, GroupResource resource, CancellationToken cancellationToken)
    {
        var editsOnlySubgroupFields = stored.Parent.HasValue
            && resource.Parent == stored.Parent
            && resource.PaymentInterval == stored.PaymentInterval
            && resource.CalendarSelectionId == stored.CalendarSelectionId;

        if (!editsOnlySubgroupFields
            || !await _groupVisibilityGuard.IsGroupVisibleAsync(stored.Parent!.Value, cancellationToken))
        {
            throw new ForbiddenException(SubgroupEditOnlyMessage);
        }
    }

    private static KeyNotFoundException GroupNotFound(Guid groupId)
    {
        return new KeyNotFoundException(string.Format(GroupNotFoundMessage, groupId));
    }
}
