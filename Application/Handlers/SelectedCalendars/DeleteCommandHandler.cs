using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.SelectedCalendars;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<SelectedCalendarResource>, SelectedCalendarResource?>
{
    private readonly SelectedCalendarApplicationService _selectedCalendarApplicationService;

    public DeleteCommandHandler(SelectedCalendarApplicationService selectedCalendarApplicationService)
    {
        _selectedCalendarApplicationService = selectedCalendarApplicationService;
    }

    public async Task<SelectedCalendarResource?> Handle(DeleteCommand<SelectedCalendarResource> request, CancellationToken cancellationToken)
    {
        await _selectedCalendarApplicationService.DeleteSelectedCalendarAsync(request.Id, cancellationToken);
        return null;
    }
}
