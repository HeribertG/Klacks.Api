using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Clients
{
    public class GetTruncatedListQueryHandler : IRequestHandler<GetTruncatedListQuery, TruncatedClientResource>
    {
        private readonly IClientRepository _clientRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetTruncatedListQueryHandler> _logger;

        public GetTruncatedListQueryHandler(IClientRepository clientRepository, IMapper mapper, ILogger<GetTruncatedListQueryHandler> logger)
        {
            _clientRepository = clientRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<TruncatedClientResource> Handle(GetTruncatedListQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching truncated client list");
            
            if (request.Filter == null)
            {
                _logger.LogWarning("Filter parameter is null for truncated client list");
                throw new InvalidRequestException("Filter parameter is required for truncated client list");
            }
            
            try
            {
                var truncatedClients = await _clientRepository.Truncated(request.Filter);
                
                _logger.LogInformation($"Retrieved truncated client list with {truncatedClients?.Clients?.Count ?? 0} clients");
                
                return _mapper.Map<TruncatedClientResource>(truncatedClients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching truncated client list");
                throw new InvalidRequestException($"Failed to retrieve truncated client list: {ex.Message}");
            }
        }
    }
}
