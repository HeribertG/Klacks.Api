using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Addresses;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<AddressResource>, AddressResource?>
{
    private readonly IAddressRepository _addressRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public DeleteCommandHandler(
        IAddressRepository addressRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _addressRepository = addressRepository;
        _mapper = mapper;
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

            var addressResource = _mapper.Map<AddressResource>(existingAddress);
            await _addressRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return addressResource;
        }, 
        "deleting address", 
        new { AddressId = request.Id });
    }
}
