// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Returns one group with its member items. The group itself stays readable for every caller; the member items of a
/// group the caller cannot see are omitted (owner decision 2026-11-07).
/// </summary>
/// <param name="groupRepository">Loads the group including its client member items</param>
/// <param name="groupVisibilityGuard">Decides whether the caller may see the group's member items</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Groups
{
    public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<GroupResource>, GroupResource>
    {
        private readonly IGroupRepository _groupRepository;
        private readonly GroupMapper _groupMapper;
        private readonly IGroupVisibilityGuard _groupVisibilityGuard;

        public GetQueryHandler(
            IGroupRepository groupRepository,
            GroupMapper groupMapper,
            IGroupVisibilityGuard groupVisibilityGuard,
            ILogger<GetQueryHandler> logger)
            : base(logger)
        {
            _groupRepository = groupRepository;
            _groupMapper = groupMapper;
            _groupVisibilityGuard = groupVisibilityGuard;
        }

        public async Task<GroupResource> Handle(GetQuery<GroupResource> request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
            {
                var group = await _groupRepository.Get(request.Id);

                if (group == null)
                {
                    throw new KeyNotFoundException($"Group with ID {request.Id} not found");
                }

                var resource = _groupMapper.ToGroupResource(group);
                await GroupMemberReadScope.HideMembersOfHiddenGroupsAsync(
                    _groupVisibilityGuard, [resource], cancellationToken);
                return resource;
            }, nameof(Handle), new { request.Id });
        }
    }
}
