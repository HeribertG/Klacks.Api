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
/// Creates a group, optionally under a parent and with initial client members, and queues it for geocoding. Nobody
/// may create a group under a hidden parent or add clients they cannot see. A caller without the Admin role (the
/// supervisor, owner decision 2026-11-08) may only create a subgroup: the parent is required and shift assignments are
/// not accepted (they go through GroupItems with their own shift checks), otherwise the write is forbidden (403); the
/// subgroup takes payment interval and holiday calendar from its parent whatever the request carries, so it is
/// plannable at once and the settings stay with the Admin. The Admin creates root groups and chooses the settings
/// freely. Skills reach this handler through the REST endpoint with the caller's token.
/// </summary>
/// <param name="groupRepository">Self-committing nested-set aware group store; also supplies the parent's settings</param>
/// <param name="groupVisibilityGuard">Decides whether the caller may write under the requested parent</param>
/// <param name="clientVisibilityGuard">Decides whether the caller may see the initial members</param>
/// <param name="userService">Tells whether the caller holds the Admin role (role claim, like the endpoint gate)</param>
public class PostCommandHandler : BaseTransactionHandler, IRequestHandler<PostCommand<GroupResource>, GroupResource?>
{
    private const string SubgroupParentRequiredMessage =
        "Only an administrator may create a root group, a group outside the visible group tree or a group with shift assignments; create the group as a subgroup of a visible group.";

    private readonly IGroupRepository _groupRepository;
    private readonly GroupMapper _groupMapper;
    private readonly IGroupGeocodingQueue _groupGeocodingQueue;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IUserService _userService;

    public PostCommandHandler(
        IGroupRepository groupRepository,
        GroupMapper groupMapper,
        IGroupGeocodingQueue groupGeocodingQueue,
        IGroupVisibilityGuard groupVisibilityGuard,
        IClientVisibilityGuard clientVisibilityGuard,
        IUserService userService,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _groupRepository = groupRepository;
        _groupMapper = groupMapper;
        _groupGeocodingQueue = groupGeocodingQueue;
        _groupVisibilityGuard = groupVisibilityGuard;
        _clientVisibilityGuard = clientVisibilityGuard;
        _userService = userService;
    }

    public async Task<GroupResource?> Handle(PostCommand<GroupResource> request, CancellationToken cancellationToken)
    {
        var created = await ExecuteWithTransactionAsync(async () =>
        {
            var inheritedSettingsSource = await EnsureCallerMayCreateAsync(request.Resource, cancellationToken);

            var group = _groupMapper.ToGroupEntity(request.Resource);
            group.Id = Guid.NewGuid();
            if (inheritedSettingsSource != null)
            {
                group.PaymentInterval = inheritedSettingsSource.PaymentInterval;
                group.CalendarSelectionId = inheritedSettingsSource.CalendarSelectionId;
            }
            await _groupRepository.Add(group);
            await _unitOfWork.CompleteAsync();
            return _groupMapper.ToGroupResource(group);
        },
        "creating group",
        new { GroupId = request.Resource?.Id });

        if (created != null)
        {
            _groupGeocodingQueue.Queue(created.Id);
        }

        return created;
    }

    private async Task<Group?> EnsureCallerMayCreateAsync(GroupResource resource, CancellationToken cancellationToken)
    {
        var isAdmin = _userService.GetRights().Contains(Roles.Admin);
        var parentHidden = resource.Parent.HasValue
            && !await _groupVisibilityGuard.IsGroupVisibleAsync(resource.Parent.Value, cancellationToken);
        var subgroupRuleBroken = !isAdmin
            && (!resource.Parent.HasValue || (resource.GroupItems ?? []).Any(item => item.ShiftId.HasValue));

        if (parentHidden || subgroupRuleBroken)
        {
            throw new ForbiddenException(SubgroupParentRequiredMessage);
        }

        var inheritedSettingsSource = isAdmin ? null : await _groupRepository.Get(resource.Parent!.Value);

        var memberClientIds = (resource.GroupItems ?? [])
            .Where(item => item.ClientId.HasValue)
            .Select(item => item.ClientId!.Value)
            .ToList();

        await GroupMemberVisibilityCheck.EnsureAddedClientsVisibleAsync(
            _clientVisibilityGuard, memberClientIds, cancellationToken);

        return inheritedSettingsSource;
    }
}
