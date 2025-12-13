using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Shifts;

public class GetShiftScheduleQueryHandler : IRequestHandler<GetShiftScheduleQuery, List<ShiftScheduleResource>>
{
    private readonly IShiftScheduleRepository _shiftScheduleRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly ILogger<GetShiftScheduleQueryHandler> _logger;

    public GetShiftScheduleQueryHandler(
        IShiftScheduleRepository shiftScheduleRepository,
        ScheduleMapper scheduleMapper,
        ILogger<GetShiftScheduleQueryHandler> logger)
    {
        _shiftScheduleRepository = shiftScheduleRepository;
        _scheduleMapper = scheduleMapper;
        _logger = logger;
    }

    public async Task<List<ShiftScheduleResource>> Handle(
        GetShiftScheduleQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling GetShiftScheduleQuery for {Month}/{Year}",
            request.Filter.CurrentMonth,
            request.Filter.CurrentYear);

        var shiftDayAssignments = await _shiftScheduleRepository.GetShiftScheduleAsync(
            request.Filter,
            cancellationToken);

        var result = _scheduleMapper.ToShiftScheduleResourceList(shiftDayAssignments);

        _logger.LogInformation("Returned {Count} shift schedule resources", result.Count);

        return result;
    }
}
