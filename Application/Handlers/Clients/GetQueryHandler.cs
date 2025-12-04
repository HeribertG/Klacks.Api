using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Clients
{
    public class GetQueryHandler : IRequestHandler<GetQuery<ClientResource>, ClientResource>
    {
        private readonly IClientRepository _clientRepository;
        private readonly ClientMapper _clientMapper;
        private readonly ILogger<GetQueryHandler> _logger;

        public GetQueryHandler(IClientRepository clientRepository, ClientMapper clientMapper, ILogger<GetQueryHandler> logger)
        {
            _clientRepository = clientRepository;
            _clientMapper = clientMapper;
            _logger = logger;
        }

        public async Task<ClientResource> Handle(GetQuery<ClientResource> request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting client with ID: {Id}", request.Id);
                
                var client = await _clientRepository.Get(request.Id);
                
                if (client == null)
                {
                    throw new KeyNotFoundException($"Client with ID {request.Id} not found");
                }
                
                var result = _clientMapper.ToResource(client);
                _logger.LogInformation("Successfully retrieved client with ID: {Id}", request.Id);
                return result;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving client with ID: {Id}", request.Id);
                throw new InvalidRequestException($"Error retrieving client with ID {request.Id}: {ex.Message}");
            }
        }
    }
}
