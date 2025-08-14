using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Addresses;

public class PostCommandHandler : IRequestHandler<PostCommand<AddressResource>, AddressResource?>
{
    private readonly IAddressRepository _addressRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostCommandHandler> _logger;

    public PostCommandHandler(
        IAddressRepository addressRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
    {
        _addressRepository = addressRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AddressResource?> Handle(PostCommand<AddressResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var address = _mapper.Map<Klacks.Api.Domain.Models.Staffs.Address>(request.Resource);
            await _addressRepository.Add(address);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<AddressResource>(address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding a new address. ID: {AddressId}", request.Resource.Id);
            throw;
        }
    }
}
