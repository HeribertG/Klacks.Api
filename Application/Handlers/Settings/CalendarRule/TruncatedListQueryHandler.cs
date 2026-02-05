using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.CalendarRules;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRules;

public class TruncatedListQueryHandler : IRequestHandler<TruncatedListQuery, TruncatedCalendarRule>
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<TruncatedListQueryHandler> _logger;

    public TruncatedListQueryHandler(ISettingsRepository settingsRepository, ILogger<TruncatedListQueryHandler> logger)
    {
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    public async Task<TruncatedCalendarRule> Handle(TruncatedListQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching truncated calendar rules list");
        
        try
        {
            var truncatedList = await _settingsRepository.GetTruncatedCalendarRuleList(request.Filter);
            
            _logger.LogInformation($"Successfully retrieved truncated calendar rules list with {truncatedList.CalendarRules?.Count ?? 0} items");
            
            return truncatedList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching truncated calendar rules");
            throw new InvalidRequestException($"Failed to retrieve truncated calendar rules: {ex.Message}");
        }
    }
}
