// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Returns the member items of one group. A missing group is not found; a group the caller cannot see is answered
/// with an empty list, because the group row itself stays readable (owner decision 2026-11-07).
/// </summary>
/// <param name="groupRepository">Loads the group including its client member items</param>
/// <param name="groupVisibilityGuard">Decides whether the caller may see the group's member items</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Groups;

public class GetGroupMembersQueryHandler : IRequestHandler<GetGroupMembersQuery, List<GroupItemResource>>
{
    private readonly IGroupRepository _groupRepository;
    private readonly GroupMapper _groupMapper;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public GetGroupMembersQueryHandler(
        IGroupRepository groupRepository, GroupMapper groupMapper, IGroupVisibilityGuard groupVisibilityGuard)
    {
        _groupRepository = groupRepository;
        _groupMapper = groupMapper;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public async Task<List<GroupItemResource>> Handle(GetGroupMembersQuery request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.Get(request.GroupId);

        if (group == null)
        {
            throw new KeyNotFoundException($"Group with ID {request.GroupId} not found");
        }

        if (!await _groupVisibilityGuard.IsGroupVisibleAsync(group.Id, cancellationToken))
        {
            return [];
        }

        return group.GroupItems.Select(g => _groupMapper.ToGroupItemResource(g)).ToList();
    }
}
