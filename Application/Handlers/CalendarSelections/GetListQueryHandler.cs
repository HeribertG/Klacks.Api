using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.CalendarSelections
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<CalendarSelectionResource>, IEnumerable<CalendarSelectionResource>>
    {
        private readonly ICalendarSelectionRepository _calendarSelectionRepository;
        private readonly ScheduleMapper _scheduleMapper;
        private readonly ILogger<GetListQueryHandler> _logger;

        public GetListQueryHandler(ICalendarSelectionRepository calendarSelectionRepository, ScheduleMapper scheduleMapper, ILogger<GetListQueryHandler> logger)
        {
            _calendarSelectionRepository = calendarSelectionRepository;
            _scheduleMapper = scheduleMapper;
            _logger = logger;
        }

        public async Task<IEnumerable<CalendarSelectionResource>> Handle(ListQuery<CalendarSelectionResource> request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Fetching calendar selections list");
                
                var calendarSelections = await _calendarSelectionRepository.List();
                var selectionsList = calendarSelections.ToList();

                _logger.LogInformation("Retrieved {Count} calendar selections", selectionsList.Count);

                return selectionsList.Select(c => _scheduleMapper.ToCalendarSelectionResource(c)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching calendar selections");
                throw new InvalidRequestException($"Failed to retrieve calendar selections: {ex.Message}");
            }
        }
    }
}
