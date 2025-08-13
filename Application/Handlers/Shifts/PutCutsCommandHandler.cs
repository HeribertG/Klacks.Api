using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Shifts;

public class PutCutsCommandHandler : IRequestHandler<PutCutsCommand, List<ShiftResource>>
{
    private readonly IShiftApplicationService _shiftApplicationService;

    public PutCutsCommandHandler(IShiftApplicationService shiftApplicationService)
    {
        _shiftApplicationService = shiftApplicationService;
    }

    public async Task<List<ShiftResource>> Handle(PutCutsCommand request, CancellationToken cancellationToken)
    {
        return await _shiftApplicationService.UpdateShiftCutsAsync(request.Cuts.ToList(), cancellationToken);
    }
}