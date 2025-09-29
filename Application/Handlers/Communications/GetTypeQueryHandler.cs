using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Communications;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Communications
{
    public class GetTypeQueryHandler : IRequestHandler<GetTypeQuery, IEnumerable<CommunicationTypeResource>>
    {
        private readonly ICommunicationRepository _communicationRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetTypeQueryHandler> _logger;

        public GetTypeQueryHandler(ICommunicationRepository communicationRepository, IMapper mapper, ILogger<GetTypeQueryHandler> logger)
        {
            _communicationRepository = communicationRepository;
            _mapper = mapper;
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
                
                return _mapper.Map<IEnumerable<CommunicationTypeResource>>(typesList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching communication types");
                throw new InvalidRequestException($"Failed to retrieve communication types: {ex.Message}");
            }
        }
    }
}
