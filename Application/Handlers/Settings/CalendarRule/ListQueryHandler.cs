using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.CalendarRules;
using Klacks.Api.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRule
{
    public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<Klacks.Api.Domain.Models.Settings.CalendarRule>>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly ILogger<ListQueryHandler> _logger;

        public ListQueryHandler(ISettingsRepository settingsRepository, ILogger<ListQueryHandler> logger)
        {
            _settingsRepository = settingsRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<Klacks.Api.Domain.Models.Settings.CalendarRule>> Handle(ListQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching calendar rules list");
            
            try
            {
                var calendarRules = await _settingsRepository.GetCalendarRuleList();
                var rulesList = calendarRules.ToList();
                
                _logger.LogInformation($"Successfully retrieved {rulesList.Count} calendar rules");
                
                return rulesList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching calendar rules");
                throw new InvalidRequestException($"Failed to retrieve calendar rules: {ex.Message}");
            }
        }
    }
}
