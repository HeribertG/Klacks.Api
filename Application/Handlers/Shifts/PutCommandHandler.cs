using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Shifts;

public class PutCommandHandler : IRequestHandler<PutCommand<ShiftResource>, ShiftResource?>
{
    private readonly IShiftApplicationService _shiftApplicationService;

    public PutCommandHandler(IShiftApplicationService shiftApplicationService)
    {
        _shiftApplicationService = shiftApplicationService;
    }

    public async Task<ShiftResource?> Handle(PutCommand<ShiftResource> request, CancellationToken cancellationToken)
    {
        return await _shiftApplicationService.UpdateShiftAsync(request.Resource, cancellationToken);
    }
}
