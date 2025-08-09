using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.SelectedCalendars
{
    public class GetQueryHandler : IRequestHandler<GetQuery<SelectedCalendarResource>, SelectedCalendarResource?>
    {
        private readonly IMapper mapper;
        private readonly ISelectedCalendarRepository repository;

        public GetQueryHandler(IMapper mapper, ISelectedCalendarRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<SelectedCalendarResource?> Handle(GetQuery<SelectedCalendarResource> request, CancellationToken cancellationToken)
        {
            var selectedCalendar = await repository.Get(request.Id);
            return mapper.Map<Models.CalendarSelections.SelectedCalendar, SelectedCalendarResource>(selectedCalendar!);
        }
    }
}
