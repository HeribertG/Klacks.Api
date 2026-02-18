using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.CalendarRules;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRule
{
    public class GetQueryHandler : IRequestHandler<GetQuery, Klacks.Api.Domain.Models.Settings.CalendarRule?>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly ILogger<GetQueryHandler> _logger;

        public GetQueryHandler(ISettingsRepository settingsRepository, ILogger<GetQueryHandler> logger)
        {
            _settingsRepository = settingsRepository;
            _logger = logger;
        }

        public async Task<Klacks.Api.Domain.Models.Settings.CalendarRule?> Handle(GetQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching calendar rule with ID: {Id}", request.Id);
            
            try
            {
                var calendarRule = await _settingsRepository.GetCalendarRule(request.Id);
                
                _logger.LogInformation("Successfully retrieved calendar rule with ID: {Id}", request.Id);
                
                return calendarRule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching calendar rule with ID: {Id}", request.Id);
                throw new InvalidRequestException($"Failed to retrieve calendar rule: {ex.Message}");
            }
        }
    }
}
