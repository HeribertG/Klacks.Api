using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Addresses;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Application.Handlers.Addresses
{
    public class GetClientAddressListQueryHandler : IRequestHandler<ClientAddressListQuery, IEnumerable<AddressResource>>
    {
        private readonly IAddressRepository _addressRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetClientAddressListQueryHandler> _logger;

        public GetClientAddressListQueryHandler(IAddressRepository addressRepository, IMapper mapper, ILogger<GetClientAddressListQueryHandler> logger)
        {
            _addressRepository = addressRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<AddressResource>> Handle(ClientAddressListQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Fetching client address list for ID: {request.Id}");
                
                if (request.Id == Guid.Empty)
                {
                    _logger.LogWarning("Invalid client ID provided: empty GUID");
                    throw new InvalidRequestException("Client ID cannot be empty");
                }
                
                var addresses = await _addressRepository.ClienList(request.Id);
                
                _logger.LogInformation($"Retrieved {addresses.Count()} addresses for client ID: {request.Id}");
                return _mapper.Map<IEnumerable<AddressResource>>(addresses);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, $"No addresses found for client ID: {request.Id}");
                throw new KeyNotFoundException($"No addresses found for client ID: {request.Id}");
            }
            catch (InvalidRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error while fetching addresses for client ID: {request.Id}");
                throw new InvalidRequestException($"Failed to retrieve addresses for client ID {request.Id}: {ex.Message}");
            }
        }
    }
}
