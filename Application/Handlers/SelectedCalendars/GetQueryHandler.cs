using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.SelectedCalendars
{
    public class GetQueryHandler : IRequestHandler<GetQuery<SelectedCalendarResource>, SelectedCalendarResource?>
    {
        private readonly SelectedCalendarApplicationService _selectedCalendarApplicationService;

        public GetQueryHandler(SelectedCalendarApplicationService selectedCalendarApplicationService)
        {
            _selectedCalendarApplicationService = selectedCalendarApplicationService;
        }

        public async Task<SelectedCalendarResource?> Handle(GetQuery<SelectedCalendarResource> request, CancellationToken cancellationToken)
        {
            return await _selectedCalendarApplicationService.GetSelectedCalendarByIdAsync(request.Id, cancellationToken);
        }
    }
}
