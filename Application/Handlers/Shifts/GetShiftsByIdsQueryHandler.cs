// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for bulk-loading shifts by a list of IDs with full client and address information.
/// </summary>
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Shifts;

public class GetShiftsByIdsQueryHandler : IRequestHandler<GetShiftsByIdsQuery, List<ShiftResource>>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly ILogger<GetShiftsByIdsQueryHandler> _logger;

    public GetShiftsByIdsQueryHandler(
        IShiftRepository shiftRepository,
        ScheduleMapper scheduleMapper,
        ILogger<GetShiftsByIdsQueryHandler> logger)
    {
        _shiftRepository = shiftRepository;
        _scheduleMapper = scheduleMapper;
        _logger = logger;
    }

    public async Task<List<ShiftResource>> Handle(GetShiftsByIdsQuery request, CancellationToken cancellationToken)
    {
        if (request.Ids == null || request.Ids.Count == 0)
        {
            return new List<ShiftResource>();
        }

        var distinctIds = request.Ids.Distinct().ToList();

        var shifts = await _shiftRepository
            .GetQueryWithClient()
            .Where(s => distinctIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Bulk-loaded {Count} shifts for {Requested} requested IDs", shifts.Count, distinctIds.Count);

        return shifts.Select(_scheduleMapper.ToShiftResource).ToList();
    }
}
