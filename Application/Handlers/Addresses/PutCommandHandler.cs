// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Updates an address. Both the stored owning client and the client named in the request must be inside the caller's
/// group visibility; otherwise the address is refused exactly like a missing one and nothing is written.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the owning client</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Addresses;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<AddressResource>, AddressResource?>
{
    private readonly IAddressRepository _addressRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly AddressCommunicationMapper _addressCommunicationMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PutCommandHandler(
        IAddressRepository addressRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        AddressCommunicationMapper addressCommunicationMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _addressRepository = addressRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _addressCommunicationMapper = addressCommunicationMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<AddressResource?> Handle(PutCommand<AddressResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingAddress = await _addressRepository.GetNoTracking(request.Resource.Id);
            if (existingAddress == null
                || !await _clientVisibilityGuard.AreAllVisibleAsync(
                    [existingAddress.ClientId, request.Resource.ClientId], cancellationToken))
            {
                throw new KeyNotFoundException($"Address with ID {request.Resource.Id} not found.");
            }

            var updatedAddress = _addressCommunicationMapper.ToAddressEntity(request.Resource);
            updatedAddress.CreateTime = existingAddress.CreateTime;
            updatedAddress.CurrentUserCreated = existingAddress.CurrentUserCreated;
            existingAddress = updatedAddress;
            await _addressRepository.Put(existingAddress);
            await _unitOfWork.CompleteAsync();
            return _addressCommunicationMapper.ToAddressResource(existingAddress);
        }, 
        "updating address", 
        new { AddressId = request.Resource.Id });
    }
}
