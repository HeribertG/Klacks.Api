using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Communications
{
    public class GetQueryHandler : IRequestHandler<GetQuery<CommunicationResource>, CommunicationResource>
    {
        private readonly ICommunicationRepository _communicationRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetQueryHandler> _logger;

        public GetQueryHandler(ICommunicationRepository communicationRepository, IMapper mapper, ILogger<GetQueryHandler> logger)
        {
            _communicationRepository = communicationRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<CommunicationResource> Handle(GetQuery<CommunicationResource> request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting communication with ID: {Id}", request.Id);
                
                var communication = await _communicationRepository.Get(request.Id);
                
                if (communication == null)
                {
                    throw new KeyNotFoundException($"Communication with ID {request.Id} not found");
                }
                
                var result = _mapper.Map<CommunicationResource>(communication);
                _logger.LogInformation("Successfully retrieved communication with ID: {Id}", request.Id);
                return result;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving communication with ID: {Id}", request.Id);
                throw new InvalidRequestException($"Error retrieving communication with ID {request.Id}: {ex.Message}");
            }
        }
    }
}
