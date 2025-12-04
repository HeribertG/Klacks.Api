using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.MacrosTypes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Settings.MacrosTypes;

public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<Klacks.Api.Domain.Models.Settings.MacroType>>
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<ListQueryHandler> _logger;

    public ListQueryHandler(ISettingsRepository settingsRepository, ILogger<ListQueryHandler> logger)
    {
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Klacks.Api.Domain.Models.Settings.MacroType>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching macro types list");
        
        try
        {
            var macroTypes = await _settingsRepository.GetOriginalMacroTypeList();
            var typesList = macroTypes.ToList();
            
            _logger.LogInformation($"Successfully retrieved {typesList.Count} macro types");
            
            return typesList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching macro types");
            throw new InvalidRequestException($"Failed to retrieve macro types: {ex.Message}");
        }
    }
}
