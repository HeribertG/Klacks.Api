// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Shifts;

public class GetShiftScheduleQueryHandler : IRequestHandler<GetShiftScheduleQuery, ShiftScheduleResponse>
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

    public async Task<ShiftScheduleResponse> Handle(
        GetShiftScheduleQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling GetShiftScheduleQuery for {StartDate} to {EndDate}",
            request.Filter.StartDate,
            request.Filter.EndDate);

        var (shiftDayAssignments, totalCount) = await _shiftScheduleRepository.GetShiftScheduleAsync(
            request.Filter,
            cancellationToken);

        var shifts = _scheduleMapper.ToShiftScheduleResourceList(shiftDayAssignments);

        _logger.LogInformation("Returned {Count} of {Total} shift schedule resources", shifts.Count, totalCount);

        return new ShiftScheduleResponse
        {
            Shifts = shifts,
            TotalCount = totalCount
        };
    }
}
