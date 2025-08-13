using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.CalendarSelections
{
    public class GetQueryHandler : IRequestHandler<GetQuery<CalendarSelectionResource>, CalendarSelectionResource?>
    {
        private readonly CalendarSelectionApplicationService _calendarSelectionApplicationService;

        public GetQueryHandler(CalendarSelectionApplicationService calendarSelectionApplicationService)
        {
            _calendarSelectionApplicationService = calendarSelectionApplicationService;
        }

        public async Task<CalendarSelectionResource?> Handle(GetQuery<CalendarSelectionResource> request, CancellationToken cancellationToken)
        {
            return await _calendarSelectionApplicationService.GetCalendarSelectionWithSelectedCalendarsAsync(request.Id, cancellationToken);
        }
    }
}
