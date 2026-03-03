// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
    private readonly AddressCommunicationMapper _addressCommunicationMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PostCommandHandler(
        IAddressRepository addressRepository,
        AddressCommunicationMapper addressCommunicationMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _addressRepository = addressRepository;
        _addressCommunicationMapper = addressCommunicationMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<AddressResource?> Handle(PostCommand<AddressResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var address = _addressCommunicationMapper.ToAddressEntity(request.Resource);
            await _addressRepository.Add(address);
            await _unitOfWork.CompleteAsync();
            return _addressCommunicationMapper.ToAddressResource(address);
        }, 
        "creating address", 
        new { AddressId = request.Resource?.Id });
    }
}
