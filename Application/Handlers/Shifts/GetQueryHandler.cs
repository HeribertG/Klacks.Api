using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Shifts;

public class GetQueryHandler : IRequestHandler<GetQuery<ShiftResource>, ShiftResource>
{
    private readonly IShiftApplicationService _shiftApplicationService;

    public GetQueryHandler(IShiftApplicationService shiftApplicationService)
    {
        _shiftApplicationService = shiftApplicationService;
    }

    public async Task<ShiftResource> Handle(GetQuery<ShiftResource> request, CancellationToken cancellationToken)
    {
        return await _shiftApplicationService.GetShiftByIdAsync(request.Id, cancellationToken) ?? new ShiftResource();
    }
}
