using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.CalendarSelections
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<CalendarSelectionResource>, IEnumerable<CalendarSelectionResource>>
    {
        private readonly ICalendarSelectionRepository _calendarSelectionRepository;
        private readonly IMapper _mapper;

        public GetListQueryHandler(ICalendarSelectionRepository calendarSelectionRepository, IMapper mapper)
        {
            _calendarSelectionRepository = calendarSelectionRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CalendarSelectionResource>> Handle(ListQuery<CalendarSelectionResource> request, CancellationToken cancellationToken)
        {
            var calendarSelections = await _calendarSelectionRepository.List();
            return _mapper.Map<IEnumerable<CalendarSelectionResource>>(calendarSelections);
        }
    }
}
