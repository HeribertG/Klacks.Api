using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.CalendarSelections
{
    public class GetQueryHandler : IRequestHandler<GetQuery<CalendarSelectionResource>, CalendarSelectionResource>
    {
        private readonly ICalendarSelectionRepository _calendarSelectionRepository;
        private readonly ScheduleMapper _scheduleMapper;
        private readonly ILogger<GetQueryHandler> _logger;

        public GetQueryHandler(ICalendarSelectionRepository calendarSelectionRepository, ScheduleMapper scheduleMapper, ILogger<GetQueryHandler> logger)
        {
            _calendarSelectionRepository = calendarSelectionRepository;
            _scheduleMapper = scheduleMapper;
            _logger = logger;
        }

        public async Task<CalendarSelectionResource> Handle(GetQuery<CalendarSelectionResource> request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting calendar selection with ID: {Id}", request.Id);
                
                var calendarSelection = await _calendarSelectionRepository.GetWithSelectedCalendars(request.Id);
                
                if (calendarSelection == null)
                {
                    throw new KeyNotFoundException($"Calendar selection with ID {request.Id} not found");
                }
                
                var result = _scheduleMapper.ToCalendarSelectionResource(calendarSelection);
                _logger.LogInformation("Successfully retrieved calendar selection with ID: {Id}", request.Id);
                return result;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving calendar selection with ID: {Id}", request.Id);
                throw new InvalidRequestException($"Error retrieving calendar selection with ID {request.Id}: {ex.Message}");
            }
        }
    }
}
