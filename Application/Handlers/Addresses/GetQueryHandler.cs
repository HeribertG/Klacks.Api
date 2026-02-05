using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Addresses;

public class GetQueryHandler : IRequestHandler<GetQuery<AddressResource>, AddressResource>
{
    private readonly IAddressRepository _addressRepository;
    private readonly AddressCommunicationMapper _addressCommunicationMapper;
    private readonly ILogger<GetQueryHandler> _logger;

    public GetQueryHandler(IAddressRepository addressRepository, AddressCommunicationMapper addressCommunicationMapper, ILogger<GetQueryHandler> logger)
    {
        _addressRepository = addressRepository;
        _addressCommunicationMapper = addressCommunicationMapper;
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
            
            var result = _addressCommunicationMapper.ToAddressResource(address);
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
