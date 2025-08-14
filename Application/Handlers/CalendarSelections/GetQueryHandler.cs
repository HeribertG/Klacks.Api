using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.CalendarSelections
{
    public class GetQueryHandler : IRequestHandler<GetQuery<CalendarSelectionResource>, CalendarSelectionResource?>
    {
        private readonly ICalendarSelectionRepository _calendarSelectionRepository;
        private readonly IMapper _mapper;

        public GetQueryHandler(ICalendarSelectionRepository calendarSelectionRepository, IMapper mapper)
        {
            _calendarSelectionRepository = calendarSelectionRepository;
            _mapper = mapper;
        }

        public async Task<CalendarSelectionResource?> Handle(GetQuery<CalendarSelectionResource> request, CancellationToken cancellationToken)
        {
            var calendarSelection = await _calendarSelectionRepository.GetWithSelectedCalendars(request.Id);
            
            if (calendarSelection == null)
            {
                return null;
            }
            
            return _mapper.Map<CalendarSelectionResource>(calendarSelection);
        }
    }
}
