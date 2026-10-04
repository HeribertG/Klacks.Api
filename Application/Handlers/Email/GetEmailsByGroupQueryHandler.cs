// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for retrieving paginated emails of all clients in a group and its subgroups. A group outside the
/// caller's group visibility is answered exactly like a group without emails; mail sent from an address that a
/// hidden client shares with a member of the group is left out.
/// @param request - Contains GroupId, Skip and Take for pagination
/// </summary>
/// <param name="groupVisibilityGuard">Decides whether the calling user may see the group</param>
/// <param name="clientVisibilityGuard">Decides which owners of a member's sender address the calling user may see</param>

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class GetEmailsByGroupQueryHandler : BaseHandler, IRequestHandler<GetEmailsByGroupQuery, ReceivedEmailListResponse>
{
    private readonly IGroupHierarchyService _groupHierarchyService;
    private readonly IEmailQueryRepository _emailQueryRepository;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ReceivedEmailMapper _mapper;

    public GetEmailsByGroupQueryHandler(
        IGroupHierarchyService groupHierarchyService,
        IEmailQueryRepository emailQueryRepository,
        IGroupVisibilityGuard groupVisibilityGuard,
        IClientVisibilityGuard clientVisibilityGuard,
        ReceivedEmailMapper mapper,
        ILogger<GetEmailsByGroupQueryHandler> logger)
        : base(logger)
    {
        _groupHierarchyService = groupHierarchyService;
        _emailQueryRepository = emailQueryRepository;
        _groupVisibilityGuard = groupVisibilityGuard;
        _clientVisibilityGuard = clientVisibilityGuard;
        _mapper = mapper;
    }

    public async Task<ReceivedEmailListResponse> Handle(GetEmailsByGroupQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (!await _groupVisibilityGuard.IsGroupVisibleAsync(request.GroupId, cancellationToken))
                return new ReceivedEmailListResponse { Items = [], TotalCount = 0, UnreadCount = 0 };

            var descendants = await _groupHierarchyService.GetDescendantsAsync(request.GroupId, includeParent: true);
            var groupIds = descendants.Select(g => g.Id).ToHashSet();

            var clientIds = await _emailQueryRepository.GetClientIdsByGroupIdsAsync(groupIds, cancellationToken);

            var emailAddresses = await ReceivedEmailVisibility.ExcludeHiddenSenderAddressesAsync(
                _emailQueryRepository,
                _clientVisibilityGuard,
                await _emailQueryRepository.GetEmailAddressesByClientIdsAsync(clientIds, cancellationToken),
                cancellationToken);

            if (emailAddresses.Count == 0)
                return new ReceivedEmailListResponse { Items = [], TotalCount = 0, UnreadCount = 0 };

            var result = await _emailQueryRepository.GetEmailsByAddressesAsync(
                EmailConstants.ClientAssignedFolder, emailAddresses, request.Skip, request.Take, cancellationToken);

            return new ReceivedEmailListResponse
            {
                Items = _mapper.ToListResources(result.Items),
                TotalCount = result.TotalCount,
                UnreadCount = result.UnreadCount
            };
        }, nameof(GetEmailsByGroupQuery));
    }
}
