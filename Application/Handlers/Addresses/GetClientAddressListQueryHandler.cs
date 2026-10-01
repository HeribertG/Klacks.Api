// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lists all addresses of one client. A client outside the caller's group visibility yields an empty list, exactly like a
/// client without addresses, and the repository is not queried.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the client</param>

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
        private readonly IClientVisibilityGuard _clientVisibilityGuard;
        private readonly AddressCommunicationMapper _addressCommunicationMapper;
        private readonly ILogger<GetClientAddressListQueryHandler> _logger;

        public GetClientAddressListQueryHandler(
            IAddressRepository addressRepository,
            IClientVisibilityGuard clientVisibilityGuard,
            AddressCommunicationMapper addressCommunicationMapper,
            ILogger<GetClientAddressListQueryHandler> logger)
        {
            _addressRepository = addressRepository;
            _clientVisibilityGuard = clientVisibilityGuard;
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

                if (!await _clientVisibilityGuard.IsVisibleAsync(request.Id, cancellationToken))
                {
                    return [];
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
