// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Deletes an address. An address owned by a client outside the caller's group visibility is refused exactly like a
/// missing one and nothing is deleted.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the owning client</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Addresses;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<AddressResource>, AddressResource?>
{
    private readonly IAddressRepository _addressRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly AddressCommunicationMapper _addressCommunicationMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public DeleteCommandHandler(
        IAddressRepository addressRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        AddressCommunicationMapper addressCommunicationMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _addressRepository = addressRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _addressCommunicationMapper = addressCommunicationMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<AddressResource?> Handle(DeleteCommand<AddressResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingAddress = await _addressRepository.Get(request.Id);
            if (existingAddress == null || !await _clientVisibilityGuard.IsVisibleAsync(existingAddress.ClientId, cancellationToken))
            {
                throw new KeyNotFoundException($"Address with ID {request.Id} not found.");
            }

            var addressResource = _addressCommunicationMapper.ToAddressResource(existingAddress);
            await _addressRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return addressResource;
        }, 
        "deleting address", 
        new { AddressId = request.Id });
    }
}
