// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for retrieving paginated emails of a specific client.
/// @param request - Contains ClientId, Skip and Take for pagination
/// </summary>

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
    private readonly ReceivedEmailMapper _mapper;

    public GetEmailsByClientQueryHandler(
        IEmailQueryRepository emailQueryRepository,
        ReceivedEmailMapper mapper,
        ILogger<GetEmailsByClientQueryHandler> logger)
        : base(logger)
    {
        _emailQueryRepository = emailQueryRepository;
        _mapper = mapper;
    }

    public async Task<ReceivedEmailListResponse> Handle(GetEmailsByClientQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
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
