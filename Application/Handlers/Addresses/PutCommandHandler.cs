using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Addresses;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<AddressResource>, AddressResource?>
{
    private readonly IAddressRepository _addressRepository;
    private readonly AddressCommunicationMapper _addressCommunicationMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PutCommandHandler(
        IAddressRepository addressRepository,
        AddressCommunicationMapper addressCommunicationMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _addressRepository = addressRepository;
        _addressCommunicationMapper = addressCommunicationMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<AddressResource?> Handle(PutCommand<AddressResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingAddress = await _addressRepository.Get(request.Resource.Id);
            if (existingAddress == null)
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
