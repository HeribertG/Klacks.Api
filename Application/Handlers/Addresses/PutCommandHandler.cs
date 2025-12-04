using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Addresses;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<AddressResource>, AddressResource?>
{
    private readonly IAddressRepository _addressRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PutCommandHandler(
        IAddressRepository addressRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _addressRepository = addressRepository;
        _mapper = mapper;
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

            _mapper.Map(request.Resource, existingAddress);
            await _addressRepository.Put(existingAddress);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<AddressResource>(existingAddress);
        }, 
        "updating address", 
        new { AddressId = request.Resource.Id });
    }
}
