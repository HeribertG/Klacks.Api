// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Shifts;

public class GetShiftSchedulePartialQueryHandler : IRequestHandler<GetShiftSchedulePartialQuery, ShiftScheduleResponse>
{
    private readonly IShiftScheduleRepository _shiftScheduleRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly ILogger<GetShiftSchedulePartialQueryHandler> _logger;

    public GetShiftSchedulePartialQueryHandler(
        IShiftScheduleRepository shiftScheduleRepository,
        ScheduleMapper scheduleMapper,
        ILogger<GetShiftSchedulePartialQueryHandler> logger)
    {
        _shiftScheduleRepository = shiftScheduleRepository;
        _scheduleMapper = scheduleMapper;
        _logger = logger;
    }

    public async Task<ShiftScheduleResponse> Handle(
        GetShiftSchedulePartialQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling GetShiftSchedulePartialQuery for {Count} shift/date pairs",
            request.Filter.ShiftDatePairs.Count);

        var shiftDayAssignments = await _shiftScheduleRepository.GetShiftSchedulePartialAsync(
            request.Filter,
            cancellationToken);

        var shifts = _scheduleMapper.ToShiftScheduleResourceList(shiftDayAssignments);

        _logger.LogInformation("Returned {Count} partial shift schedule resources", shifts.Count);

        return new ShiftScheduleResponse
        {
            Shifts = shifts,
            TotalCount = shifts.Count
        };
    }
}
