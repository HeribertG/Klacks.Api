using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.SelectedCalendars
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<SelectedCalendarResource>, IEnumerable<SelectedCalendarResource>>
    {
        private readonly SelectedCalendarApplicationService _selectedCalendarApplicationService;

        public GetListQueryHandler(SelectedCalendarApplicationService selectedCalendarApplicationService)
        {
            _selectedCalendarApplicationService = selectedCalendarApplicationService;
        }

        public async Task<IEnumerable<SelectedCalendarResource>> Handle(ListQuery<SelectedCalendarResource> request, CancellationToken cancellationToken)
        {
            return await _selectedCalendarApplicationService.GetAllSelectedCalendarsAsync(cancellationToken);
        }
    }
}
