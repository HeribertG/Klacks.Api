// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.CalendarSelections
{
    public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<CalendarSelectionResource>, CalendarSelectionResource>
    {
        private readonly ICalendarSelectionRepository _calendarSelectionRepository;
        private readonly ScheduleMapper _scheduleMapper;

        public GetQueryHandler(ICalendarSelectionRepository calendarSelectionRepository, ScheduleMapper scheduleMapper, ILogger<GetQueryHandler> logger)
            : base(logger)
        {
            _calendarSelectionRepository = calendarSelectionRepository;
            _scheduleMapper = scheduleMapper;
        }

        public async Task<CalendarSelectionResource> Handle(GetQuery<CalendarSelectionResource> request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
            {
                var calendarSelection = await _calendarSelectionRepository.GetWithSelectedCalendars(request.Id);

                if (calendarSelection == null)
                {
                    throw new KeyNotFoundException($"Calendar selection with ID {request.Id} not found");
                }

                return _scheduleMapper.ToCalendarSelectionResource(calendarSelection);
            }, nameof(Handle), new { request.Id });
        }
    }
}
