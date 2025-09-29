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
    public class GetSimpleAddressListQueryHandler : IRequestHandler<GetSimpleAddressListQuery, IEnumerable<AddressResource>>
    {
        private readonly IAddressRepository _addressRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetSimpleAddressListQueryHandler> _logger;

        public GetSimpleAddressListQueryHandler(IAddressRepository addressRepository, IMapper mapper, ILogger<GetSimpleAddressListQueryHandler> logger)
        {
            _addressRepository = addressRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<AddressResource>> Handle(GetSimpleAddressListQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Fetching simple address list for ID: {request.Id}");
                
                if (request.Id == Guid.Empty)
                {
                    _logger.LogWarning("Invalid ID provided for simple address list: empty GUID");
                    throw new InvalidRequestException("ID cannot be empty for simple address list query");
                }
                
                var addresses = await _addressRepository.SimpleList(request.Id);
                
                _logger.LogInformation($"Retrieved {addresses.Count()} simple addresses for ID: {request.Id}");
                return _mapper.Map<IEnumerable<AddressResource>>(addresses);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, $"No addresses found for ID: {request.Id}");
                throw new KeyNotFoundException($"No addresses found for ID: {request.Id}");
            }
            catch (InvalidRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error while fetching simple addresses for ID: {request.Id}");
                throw new InvalidRequestException($"Failed to retrieve simple addresses for ID {request.Id}: {ex.Message}");
            }
        }
    }
}
