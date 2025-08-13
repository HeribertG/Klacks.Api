using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.CalendarSelections
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<CalendarSelectionResource>, IEnumerable<CalendarSelectionResource>>
    {
        private readonly CalendarSelectionApplicationService _calendarSelectionApplicationService;

        public GetListQueryHandler(CalendarSelectionApplicationService calendarSelectionApplicationService)
        {
            _calendarSelectionApplicationService = calendarSelectionApplicationService;
        }

        public async Task<IEnumerable<CalendarSelectionResource>> Handle(ListQuery<CalendarSelectionResource> request, CancellationToken cancellationToken)
        {
            return await _calendarSelectionApplicationService.GetAllCalendarSelectionsAsync(cancellationToken);
        }
    }
}
