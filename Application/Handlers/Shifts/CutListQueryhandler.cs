using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Shifts;

public class CutListQueryhandler : IRequestHandler<CutListQuery, IEnumerable<ShiftResource>>
{
    private readonly IShiftApplicationService _shiftApplicationService;

    public CutListQueryhandler(IShiftApplicationService shiftApplicationService)
    {
        _shiftApplicationService = shiftApplicationService;
    }

    public async Task<IEnumerable<ShiftResource>> Handle(CutListQuery request, CancellationToken cancellationToken)
    {
        return await _shiftApplicationService.GetShiftCutsAsync(request.Id, cancellationToken);
    }
}
