using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Addresses;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<AddressResource>, AddressResource?>
{
    private readonly IAddressRepository _addressRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCommandHandler> _logger;

    public DeleteCommandHandler(
        IAddressRepository addressRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
    {
        _addressRepository = addressRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AddressResource?> Handle(DeleteCommand<AddressResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingAddress = await _addressRepository.Get(request.Id);
            if (existingAddress == null)
            {
                _logger.LogWarning("Address with ID {AddressId} not found for deletion.", request.Id);
                return null;
            }

            var addressResource = _mapper.Map<AddressResource>(existingAddress);
            await _addressRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return addressResource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting address with ID {AddressId}.", request.Id);
            throw;
        }
    }
}
