using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Communications;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Communications
{
    public class GetTypeQueryHandler : IRequestHandler<GetTypeQuery, IEnumerable<CommunicationTypeResource>>
    {
        private readonly ICommunicationRepository _communicationRepository;
        private readonly AddressCommunicationMapper _addressCommunicationMapper;
        private readonly ILogger<GetTypeQueryHandler> _logger;

        public GetTypeQueryHandler(ICommunicationRepository communicationRepository, AddressCommunicationMapper addressCommunicationMapper, ILogger<GetTypeQueryHandler> logger)
        {
            _communicationRepository = communicationRepository;
            _addressCommunicationMapper = addressCommunicationMapper;
            _logger = logger;
        }

        public async Task<IEnumerable<CommunicationTypeResource>> Handle(GetTypeQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching communication types list");
            
            try
            {
                var communicationTypes = await _communicationRepository.TypeList();
                var typesList = communicationTypes.ToList();
                
                _logger.LogInformation($"Retrieved {typesList.Count} communication types");
                
                return _addressCommunicationMapper.ToCommunicationTypeResources(typesList.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching communication types");
                throw new InvalidRequestException($"Failed to retrieve communication types: {ex.Message}");
            }
        }
    }
}
