using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Shifts;

public class PostResetCutsCommandHandler : BaseHandler, IRequestHandler<PostResetCutsCommand, List<ShiftResource>>
{
    private readonly IShiftCutFacade _shiftCutFacade;
    private readonly ScheduleMapper _scheduleMapper;

    public PostResetCutsCommandHandler(
        IShiftCutFacade shiftCutFacade,
        ScheduleMapper scheduleMapper,
        ILogger<PostResetCutsCommandHandler> logger)
        : base(logger)
    {
        _shiftCutFacade = shiftCutFacade;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<List<ShiftResource>> Handle(PostResetCutsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PostResetCutsCommand received for OriginalId={OriginalId}, NewStartDate={NewStartDate}",
            request.OriginalId, request.NewStartDate);

        var updatedShifts = await _shiftCutFacade.ResetCutsAsync(request.OriginalId, request.NewStartDate);

        var results = updatedShifts.Select(s => _scheduleMapper.ToShiftResource(s)).ToList();

        _logger.LogInformation("PostResetCutsCommand completed - returned {Count} shifts", results.Count);
        return results;
    }
}
