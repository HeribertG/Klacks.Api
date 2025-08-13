using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Shifts;

public class PostCommandHandler : IRequestHandler<PostCommand<ShiftResource>, ShiftResource?>
{
    private readonly IShiftApplicationService _shiftApplicationService;

    public PostCommandHandler(IShiftApplicationService shiftApplicationService)
    {
        _shiftApplicationService = shiftApplicationService;
    }

    public async Task<ShiftResource?> Handle(PostCommand<ShiftResource> request, CancellationToken cancellationToken)
    {
        return await _shiftApplicationService.CreateShiftAsync(request.Resource, cancellationToken);
    }
}
