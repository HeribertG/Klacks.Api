using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Shifts;

public class PostBatchCutsCommandHandler : BaseHandler, IRequestHandler<PostBatchCutsCommand, List<ShiftResource>>
{
    private readonly IShiftCutFacade _shiftCutFacade;
    private readonly ScheduleMapper _scheduleMapper;

    public PostBatchCutsCommandHandler(
        IShiftCutFacade shiftCutFacade,
        ScheduleMapper scheduleMapper,
        ILogger<PostBatchCutsCommandHandler> logger)
        : base(logger)
    {
        _shiftCutFacade = shiftCutFacade;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<List<ShiftResource>> Handle(PostBatchCutsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PostBatchCutsCommand received with {Count} operations", request.Operations.Count);

        var processedShifts = await _shiftCutFacade.ProcessBatchCutsAsync(request.Operations);

        var results = processedShifts.Select(s => _scheduleMapper.ToShiftResource(s)).ToList();

        _logger.LogInformation("PostBatchCutsCommand completed - returned {Count} shifts", results.Count);
        return results;
    }
}
