// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads one address. An address owned by a client outside the caller's group visibility is answered exactly
/// like an address that does not exist.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the owning client</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Addresses;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<AddressResource>, AddressResource>
{
    private readonly IAddressRepository _addressRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly AddressCommunicationMapper _addressCommunicationMapper;

    public GetQueryHandler(
        IAddressRepository addressRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        AddressCommunicationMapper addressCommunicationMapper,
        ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _addressRepository = addressRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _addressCommunicationMapper = addressCommunicationMapper;
    }

    public async Task<AddressResource> Handle(GetQuery<AddressResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var address = await _addressRepository.Get(request.Id);

            if (address == null || !await _clientVisibilityGuard.IsVisibleAsync(address.ClientId, cancellationToken))
            {
                throw new KeyNotFoundException($"Address with ID {request.Id} not found");
            }

            return _addressCommunicationMapper.ToAddressResource(address);
        }, nameof(Handle), new { request.Id });
    }
}
