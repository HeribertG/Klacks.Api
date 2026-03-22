// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for retrieving paginated emails of all clients in a group and its subgroups.
/// @param request - Contains GroupId, Skip and Take for pagination
/// </summary>

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
    private readonly ReceivedEmailMapper _mapper;

    public GetEmailsByGroupQueryHandler(
        IGroupHierarchyService groupHierarchyService,
        IEmailQueryRepository emailQueryRepository,
        ReceivedEmailMapper mapper,
        ILogger<GetEmailsByGroupQueryHandler> logger)
        : base(logger)
    {
        _groupHierarchyService = groupHierarchyService;
        _emailQueryRepository = emailQueryRepository;
        _mapper = mapper;
    }

    public async Task<ReceivedEmailListResponse> Handle(GetEmailsByGroupQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var descendants = await _groupHierarchyService.GetDescendantsAsync(request.GroupId, includeParent: true);
            var groupIds = descendants.Select(g => g.Id).ToHashSet();

            var clientIds = await _emailQueryRepository.GetClientIdsByGroupIdsAsync(groupIds, cancellationToken);

            var emailAddresses = await _emailQueryRepository.GetEmailAddressesByClientIdsAsync(clientIds, cancellationToken);

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
