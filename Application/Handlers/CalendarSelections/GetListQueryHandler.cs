using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.CalendarSelections
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<CalendarSelectionResource>, IEnumerable<CalendarSelectionResource>>
    {
        private readonly ICalendarSelectionRepository _calendarSelectionRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetListQueryHandler> _logger;

        public GetListQueryHandler(ICalendarSelectionRepository calendarSelectionRepository, IMapper mapper, ILogger<GetListQueryHandler> logger)
        {
            _calendarSelectionRepository = calendarSelectionRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<CalendarSelectionResource>> Handle(ListQuery<CalendarSelectionResource> request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Fetching calendar selections list");
                
                var calendarSelections = await _calendarSelectionRepository.List();
                var selectionsList = calendarSelections.ToList();
                
                _logger.LogInformation($"Retrieved {selectionsList.Count} calendar selections");
                
                return _mapper.Map<IEnumerable<CalendarSelectionResource>>(selectionsList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching calendar selections");
                throw new InvalidRequestException($"Failed to retrieve calendar selections: {ex.Message}");
            }
        }
    }
}
