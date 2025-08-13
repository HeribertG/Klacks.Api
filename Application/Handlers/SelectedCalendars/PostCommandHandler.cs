using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.SelectedCalendars;

public class PostCommandHandler : IRequestHandler<PostCommand<SelectedCalendarResource>, SelectedCalendarResource?>
{
    private readonly SelectedCalendarApplicationService _selectedCalendarApplicationService;

    public PostCommandHandler(SelectedCalendarApplicationService selectedCalendarApplicationService)
    {
        _selectedCalendarApplicationService = selectedCalendarApplicationService;
    }

    public async Task<SelectedCalendarResource?> Handle(PostCommand<SelectedCalendarResource> request, CancellationToken cancellationToken)
    {
        return await _selectedCalendarApplicationService.CreateSelectedCalendarAsync(request.Resource, cancellationToken);
    }
}
