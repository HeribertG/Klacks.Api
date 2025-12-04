using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Macros;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Settings.Macro
{
    public class GetQueryHandler : IRequestHandler<GetQuery, MacroResource?>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly SettingsMapper _settingsMapper;
        private readonly ILogger<GetQueryHandler> _logger;

        public GetQueryHandler(ISettingsRepository settingsRepository, SettingsMapper settingsMapper, ILogger<GetQueryHandler> logger)
        {
            _settingsRepository = settingsRepository;
            _settingsMapper = settingsMapper;
            _logger = logger;
        }

        public async Task<MacroResource?> Handle(GetQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Fetching macro with ID: {request.Id}");
            
            try
            {
                var macro = await _settingsRepository.GetMacro(request.Id);
                
                _logger.LogInformation($"Successfully retrieved macro with ID: {request.Id}");
                
                return _settingsMapper.ToMacroResource(macro);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error while fetching macro with ID: {request.Id}");
                throw new InvalidRequestException($"Failed to retrieve macro: {ex.Message}");
            }
        }
    }
}
