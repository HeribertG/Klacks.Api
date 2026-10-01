// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates an address. An address for a client outside the caller's group visibility is refused exactly like one for a
/// client that does not exist, and nothing is written.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the owning client</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Addresses;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<AddressResource>, AddressResource?>
{
    private readonly IAddressRepository _addressRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly AddressCommunicationMapper _addressCommunicationMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PostCommandHandler(
        IAddressRepository addressRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        AddressCommunicationMapper addressCommunicationMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _addressRepository = addressRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _addressCommunicationMapper = addressCommunicationMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<AddressResource?> Handle(PostCommand<AddressResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (!await _clientVisibilityGuard.IsVisibleAsync(request.Resource.ClientId, cancellationToken))
            {
                throw new KeyNotFoundException($"Client with ID {request.Resource.ClientId} not found");
            }

            var address = _addressCommunicationMapper.ToAddressEntity(request.Resource);
            await _addressRepository.Add(address);
            await _unitOfWork.CompleteAsync();
            return _addressCommunicationMapper.ToAddressResource(address);
        }, 
        "creating address", 
        new { AddressId = request.Resource?.Id });
    }
}
