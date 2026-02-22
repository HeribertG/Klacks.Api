// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.CalendarRules;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Infrastructure.Mediator;
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
                
                _logger.LogInformation("Successfully retrieved {Count} calendar rules", rulesList.Count);
                
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
