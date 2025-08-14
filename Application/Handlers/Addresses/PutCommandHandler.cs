using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Addresses;

public class PutCommandHandler : IRequestHandler<PutCommand<AddressResource>, AddressResource?>
{
    private readonly IAddressRepository _addressRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PutCommandHandler> _logger;

    public PutCommandHandler(
        IAddressRepository addressRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
    {
        _addressRepository = addressRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AddressResource?> Handle(PutCommand<AddressResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingAddress = await _addressRepository.Get(request.Resource.Id);
            if (existingAddress == null)
            {
                _logger.LogWarning("Address with ID {AddressId} not found.", request.Resource.Id);
                return null;
            }

            var address = _mapper.Map<Klacks.Api.Domain.Models.Staffs.Address>(request.Resource);
            var updatedAddress = await _addressRepository.Put(address);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<AddressResource>(updatedAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating address with ID {AddressId}.", request.Resource.Id);
            throw;
        }
    }
}
