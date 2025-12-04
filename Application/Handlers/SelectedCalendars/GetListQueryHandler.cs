using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.SelectedCalendars
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<SelectedCalendarResource>, IEnumerable<SelectedCalendarResource>>
    {
        private readonly ISelectedCalendarRepository _selectedCalendarRepository;
        private readonly IMapper _mapper;

        public GetListQueryHandler(ISelectedCalendarRepository selectedCalendarRepository, IMapper mapper)
        {
            _selectedCalendarRepository = selectedCalendarRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<SelectedCalendarResource>> Handle(ListQuery<SelectedCalendarResource> request, CancellationToken cancellationToken)
        {
            var selectedCalendars = await _selectedCalendarRepository.List();
            return _mapper.Map<IEnumerable<SelectedCalendarResource>>(selectedCalendars);
        }
    }
}
