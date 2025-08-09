using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Handlers.CalendarSelections
{
    public class GetQueryHandler : IRequestHandler<GetQuery<CalendarSelectionResource>, CalendarSelectionResource?>
    {
        private readonly IMapper mapper;
        private readonly ICalendarSelectionRepository repository;

        public GetQueryHandler(IMapper mapper, ICalendarSelectionRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<CalendarSelectionResource?> Handle(GetQuery<CalendarSelectionResource> request, CancellationToken cancellationToken)
        {
            var calendarSelection = await repository.GetWithSelectedCalendars(request.Id);
            return mapper.Map<Models.CalendarSelections.CalendarSelection, CalendarSelectionResource>(calendarSelection!);
        }
    }
}
