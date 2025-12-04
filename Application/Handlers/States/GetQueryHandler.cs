using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.States
{
    public class GetQueryHandler : IRequestHandler<GetQuery<StateResource>, StateResource>
    {
        private readonly IStateRepository _stateRepository;
        private readonly SettingsMapper _settingsMapper;
        private readonly ILogger<GetQueryHandler> _logger;

        public GetQueryHandler(IStateRepository stateRepository, SettingsMapper settingsMapper, ILogger<GetQueryHandler> logger)
        {
            _stateRepository = stateRepository;
            _settingsMapper = settingsMapper;
            _logger = logger;
        }

        public async Task<StateResource> Handle(GetQuery<StateResource> request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting state with ID: {Id}", request.Id);
                
                var state = await _stateRepository.Get(request.Id);
                
                if (state == null)
                {
                    throw new KeyNotFoundException($"State with ID {request.Id} not found");
                }
                
                var result = _settingsMapper.ToStateResource(state);
                _logger.LogInformation("Successfully retrieved state with ID: {Id}", request.Id);
                return result;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving state with ID: {Id}", request.Id);
                throw new InvalidRequestException($"Error retrieving state with ID {request.Id}: {ex.Message}");
            }
        }
    }
}
