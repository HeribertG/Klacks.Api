using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Macros;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Settings.Macros;

public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<MacroResource>>
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly ILogger<ListQueryHandler> _logger;

    public ListQueryHandler(ISettingsRepository settingsRepository, SettingsMapper settingsMapper, ILogger<ListQueryHandler> logger)
    {
        _settingsRepository = settingsRepository;
        _settingsMapper = settingsMapper;
        _logger = logger;
    }

    public async Task<IEnumerable<MacroResource>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching macros list");
        
        try
        {
            var macros = await _settingsRepository.GetMacroList();
            var macrosList = macros.ToList();
            
            _logger.LogInformation($"Successfully retrieved {macrosList.Count} macros");
            
            return _settingsMapper.ToMacroResources(macrosList.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching macros");
            throw new InvalidRequestException($"Failed to retrieve macros: {ex.Message}");
        }
    }
}
