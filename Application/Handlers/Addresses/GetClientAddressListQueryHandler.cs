// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Addresses;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Application.Handlers.Addresses
{
    public class GetClientAddressListQueryHandler : IRequestHandler<ClientAddressListQuery, IEnumerable<AddressResource>>
    {
        private readonly IAddressRepository _addressRepository;
        private readonly AddressCommunicationMapper _addressCommunicationMapper;
        private readonly ILogger<GetClientAddressListQueryHandler> _logger;

        public GetClientAddressListQueryHandler(IAddressRepository addressRepository, AddressCommunicationMapper addressCommunicationMapper, ILogger<GetClientAddressListQueryHandler> logger)
        {
            _addressRepository = addressRepository;
            _addressCommunicationMapper = addressCommunicationMapper;
            _logger = logger;
        }

        public async Task<IEnumerable<AddressResource>> Handle(ClientAddressListQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Fetching client address list for ID: {Id}", request.Id);
                
                if (request.Id == Guid.Empty)
                {
                    _logger.LogWarning("Invalid client ID provided: empty GUID");
                    throw new InvalidRequestException("Client ID cannot be empty");
                }
                
                var addresses = await _addressRepository.ClienList(request.Id);
                
                _logger.LogInformation("Retrieved {Count} addresses for client ID: {Id}", addresses.Count(), request.Id);
                return _addressCommunicationMapper.ToAddressResources(addresses.ToList());
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "No addresses found for client ID: {Id}", request.Id);
                throw new KeyNotFoundException($"No addresses found for client ID: {request.Id}");
            }
            catch (InvalidRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching addresses for client ID: {Id}", request.Id);
                throw new InvalidRequestException($"Failed to retrieve addresses for client ID {request.Id}: {ex.Message}");
            }
        }
    }
}
