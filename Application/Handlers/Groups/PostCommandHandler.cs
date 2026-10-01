// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Groups;

/// <summary>
/// Creates a group, optionally under a parent and with initial client members, and queues it for geocoding.
/// A group-restricted caller may only create groups under a visible parent (a hidden parent is answered like
/// a missing one) and may not add clients they cannot see. Creating a root group stays open to every caller
/// allowed on the endpoint: an empty root makes nobody visible.
/// </summary>
/// <param name="groupRepository">Self-committing nested-set aware group store</param>
/// <param name="groupVisibilityGuard">Decides whether the caller may write under the requested parent</param>
/// <param name="clientVisibilityGuard">Decides whether the caller may see the initial members</param>
public class PostCommandHandler : BaseTransactionHandler, IRequestHandler<PostCommand<GroupResource>, GroupResource?>
{
    private const string ParentNotFoundMessage = "Parent group with ID {0} not found";

    private readonly IGroupRepository _groupRepository;
    private readonly GroupMapper _groupMapper;
    private readonly IGroupGeocodingQueue _groupGeocodingQueue;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;

    public PostCommandHandler(
        IGroupRepository groupRepository,
        GroupMapper groupMapper,
        IGroupGeocodingQueue groupGeocodingQueue,
        IGroupVisibilityGuard groupVisibilityGuard,
        IClientVisibilityGuard clientVisibilityGuard,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _groupRepository = groupRepository;
        _groupMapper = groupMapper;
        _groupGeocodingQueue = groupGeocodingQueue;
        _groupVisibilityGuard = groupVisibilityGuard;
        _clientVisibilityGuard = clientVisibilityGuard;
    }

    public async Task<GroupResource?> Handle(PostCommand<GroupResource> request, CancellationToken cancellationToken)
    {
        var created = await ExecuteWithTransactionAsync(async () =>
        {
            await EnsureCallerMayCreateAsync(request.Resource, cancellationToken);

            var group = _groupMapper.ToGroupEntity(request.Resource);
            group.Id = Guid.NewGuid();
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

    private async Task EnsureCallerMayCreateAsync(GroupResource resource, CancellationToken cancellationToken)
    {
        if (resource.Parent.HasValue)
        {
            if (!await _groupVisibilityGuard.IsGroupVisibleAsync(resource.Parent.Value, cancellationToken))
            {
                throw new KeyNotFoundException(string.Format(ParentNotFoundMessage, resource.Parent.Value));
            }
        }

        var memberClientIds = (resource.GroupItems ?? [])
            .Where(item => item.ClientId.HasValue)
            .Select(item => item.ClientId!.Value)
            .ToList();

        await GroupMemberVisibilityCheck.EnsureAddedClientsVisibleAsync(
            _clientVisibilityGuard, memberClientIds, cancellationToken);
    }
}
