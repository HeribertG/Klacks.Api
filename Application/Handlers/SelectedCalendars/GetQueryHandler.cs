// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.SelectedCalendars
{
    public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<SelectedCalendarResource>, SelectedCalendarResource>
    {
        private readonly ISelectedCalendarRepository _selectedCalendarRepository;
        private readonly ScheduleMapper _scheduleMapper;

        public GetQueryHandler(ISelectedCalendarRepository selectedCalendarRepository, ScheduleMapper scheduleMapper, ILogger<GetQueryHandler> logger)
            : base(logger)
        {
            _selectedCalendarRepository = selectedCalendarRepository;
            _scheduleMapper = scheduleMapper;
        }

        public async Task<SelectedCalendarResource> Handle(GetQuery<SelectedCalendarResource> request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
            {
                var selectedCalendar = await _selectedCalendarRepository.Get(request.Id);

                if (selectedCalendar == null)
                {
                    throw new KeyNotFoundException($"Selected calendar with ID {request.Id} not found");
                }

                return _scheduleMapper.ToSelectedCalendarResource(selectedCalendar);
            }, nameof(Handle), new { request.Id });
        }
    }
}
