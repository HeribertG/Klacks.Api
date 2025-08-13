using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Shifts;

public class PostCutsCommandHandler : IRequestHandler<PostCutsCommand, List<ShiftResource>>
{
    private readonly IShiftApplicationService _shiftApplicationService;

    public PostCutsCommandHandler(IShiftApplicationService shiftApplicationService)
    {
        _shiftApplicationService = shiftApplicationService;
    }

    public async Task<List<ShiftResource>> Handle(PostCutsCommand request, CancellationToken cancellationToken)
    {
        return await _shiftApplicationService.CreateShiftCutsAsync(request.Cuts.ToList(), cancellationToken);
    }
}