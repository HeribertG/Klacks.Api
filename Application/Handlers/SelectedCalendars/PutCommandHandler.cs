using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.SelectedCalendars;

public class PutCommandHandler : IRequestHandler<PutCommand<SelectedCalendarResource>, SelectedCalendarResource?>
{
    private readonly SelectedCalendarApplicationService _selectedCalendarApplicationService;

    public PutCommandHandler(SelectedCalendarApplicationService selectedCalendarApplicationService)
    {
        _selectedCalendarApplicationService = selectedCalendarApplicationService;
    }

    public async Task<SelectedCalendarResource?> Handle(PutCommand<SelectedCalendarResource> request, CancellationToken cancellationToken)
    {
        return await _selectedCalendarApplicationService.UpdateSelectedCalendarAsync(request.Resource, cancellationToken);
    }
}
