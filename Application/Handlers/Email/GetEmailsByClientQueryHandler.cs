// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for retrieving paginated emails of a specific client. A client outside the caller's group
/// visibility is answered exactly like a client without emails.
/// @param request - Contains ClientId, Skip and Take for pagination
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the client</param>

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class GetEmailsByClientQueryHandler : BaseHandler, IRequestHandler<GetEmailsByClientQuery, ReceivedEmailListResponse>
{
    private readonly IEmailQueryRepository _emailQueryRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ReceivedEmailMapper _mapper;

    public GetEmailsByClientQueryHandler(
        IEmailQueryRepository emailQueryRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ReceivedEmailMapper mapper,
        ILogger<GetEmailsByClientQueryHandler> logger)
        : base(logger)
    {
        _emailQueryRepository = emailQueryRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _mapper = mapper;
    }

    public async Task<ReceivedEmailListResponse> Handle(GetEmailsByClientQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (!await _clientVisibilityGuard.IsVisibleAsync(request.ClientId, cancellationToken))
                return new ReceivedEmailListResponse { Items = [], TotalCount = 0, UnreadCount = 0 };

            var emailAddresses = await _emailQueryRepository.GetEmailAddressesByClientAsync(request.ClientId, cancellationToken);

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
        }, nameof(GetEmailsByClientQuery));
    }
}
