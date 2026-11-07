// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for retrieving a single group item by ID. An item of a group the caller cannot see is answered exactly like
/// a missing item (owner decision 2026-11-07: member lists are person data).
/// </summary>
/// <param name="request">The GetQuery containing the group item ID to retrieve.</param>
/// <param name="groupVisibilityGuard">Decides whether the caller may see the item's group</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.GroupItems;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<GroupItemResource>, GroupItemResource?>
{
    private const string GroupItemNotFoundMessage = "GroupItem with ID {0} not found.";

    private readonly IGroupItemRepository _groupItemRepository;
    private readonly GroupMapper _groupMapper;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public GetQueryHandler(
        IGroupItemRepository groupItemRepository,
        GroupMapper groupMapper,
        IGroupVisibilityGuard groupVisibilityGuard,
        ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _groupItemRepository = groupItemRepository;
        _groupMapper = groupMapper;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public async Task<GroupItemResource?> Handle(GetQuery<GroupItemResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var groupItem = await _groupItemRepository.Get(request.Id);
            if (groupItem == null
                || !await _groupVisibilityGuard.IsGroupVisibleAsync(groupItem.GroupId, cancellationToken))
            {
                throw new KeyNotFoundException(string.Format(GroupItemNotFoundMessage, request.Id));
            }
            return _groupMapper.ToGroupItemResource(groupItem);
        }, nameof(Handle), new { request.Id });
    }
}
