using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.SelectedCalendars
{
    public class GetQueryHandler : IRequestHandler<GetQuery<SelectedCalendarResource>, SelectedCalendarResource?>
    {
        private readonly ISelectedCalendarRepository _selectedCalendarRepository;
        private readonly IMapper _mapper;

        public GetQueryHandler(ISelectedCalendarRepository selectedCalendarRepository, IMapper mapper)
        {
            _selectedCalendarRepository = selectedCalendarRepository;
            _mapper = mapper;
        }

        public async Task<SelectedCalendarResource?> Handle(GetQuery<SelectedCalendarResource> request, CancellationToken cancellationToken)
        {
            var selectedCalendar = await _selectedCalendarRepository.Get(request.Id);
            return selectedCalendar != null ? _mapper.Map<SelectedCalendarResource>(selectedCalendar) : null;
        }
    }
}
