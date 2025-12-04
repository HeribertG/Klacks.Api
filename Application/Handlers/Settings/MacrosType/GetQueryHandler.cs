using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.MacrosTypes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Settings.MacrosTypes;

public class GetQueryHandler : IRequestHandler<GetQuery, Klacks.Api.Domain.Models.Settings.MacroType?>
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<GetQueryHandler> _logger;

    public GetQueryHandler(ISettingsRepository settingsRepository, ILogger<GetQueryHandler> logger)
    {
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    public async Task<Klacks.Api.Domain.Models.Settings.MacroType?> Handle(GetQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Fetching macro type with ID: {request.Id}");
        
        try
        {
            var macroType = await _settingsRepository.GetMacroType(request.Id);
            
            _logger.LogInformation($"Successfully retrieved macro type with ID: {request.Id}");
            
            return macroType;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error while fetching macro type with ID: {request.Id}");
            throw new InvalidRequestException($"Failed to retrieve macro type: {ex.Message}");
        }
    }
}
