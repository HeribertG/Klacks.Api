// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.SelectedCalendars
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<SelectedCalendarResource>, IEnumerable<SelectedCalendarResource>>
    {
        private readonly ISelectedCalendarRepository _selectedCalendarRepository;
        private readonly ScheduleMapper _scheduleMapper;

        public GetListQueryHandler(ISelectedCalendarRepository selectedCalendarRepository, ScheduleMapper scheduleMapper)
        {
            _selectedCalendarRepository = selectedCalendarRepository;
            _scheduleMapper = scheduleMapper;
        }

        public async Task<IEnumerable<SelectedCalendarResource>> Handle(ListQuery<SelectedCalendarResource> request, CancellationToken cancellationToken)
        {
            var selectedCalendars = await _selectedCalendarRepository.List();
            return selectedCalendars.Select(s => _scheduleMapper.ToSelectedCalendarResource(s)).ToList();
        }
    }
}
