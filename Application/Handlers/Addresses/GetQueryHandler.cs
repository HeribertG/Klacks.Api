using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Addresses;

public class GetQueryHandler : IRequestHandler<GetQuery<AddressResource>, AddressResource>
{
    private readonly IAddressRepository _addressRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetQueryHandler> _logger;

    public GetQueryHandler(IAddressRepository addressRepository, IMapper mapper, ILogger<GetQueryHandler> logger)
    {
        _addressRepository = addressRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AddressResource> Handle(GetQuery<AddressResource> request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting address with ID: {Id}", request.Id);
            
            var address = await _addressRepository.Get(request.Id);
            
            if (address == null)
            {
                throw new KeyNotFoundException($"Address with ID {request.Id} not found");
            }
            
            var result = _mapper.Map<AddressResource>(address);
            _logger.LogInformation("Successfully retrieved address with ID: {Id}", request.Id);
            return result;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving address with ID: {Id}", request.Id);
            throw new InvalidRequestException($"Error retrieving address with ID {request.Id}: {ex.Message}");
        }
    }
}
