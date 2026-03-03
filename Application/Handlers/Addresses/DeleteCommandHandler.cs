// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
    private readonly AddressCommunicationMapper _addressCommunicationMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public DeleteCommandHandler(
        IAddressRepository addressRepository,
        AddressCommunicationMapper addressCommunicationMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _addressRepository = addressRepository;
        _addressCommunicationMapper = addressCommunicationMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<AddressResource?> Handle(DeleteCommand<AddressResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingAddress = await _addressRepository.Get(request.Id);
            if (existingAddress == null)
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
