// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for bulk-loading shifts by a list of IDs with full client and address information. Ids of shifts in
/// groups the caller may not see are dropped silently, exactly like unknown ids; group-less shifts stay visible.
/// </summary>
/// <param name="shiftRepository">Provides the shift query including client and addresses</param>
/// <param name="shiftGroupFilterService">Resolves the caller's group restriction (null = unrestricted)</param>
/// <param name="scheduleMapper">Maps the shifts to the API resource</param>
/// <param name="logger">Diagnostics logging</param>
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Services.Shifts;
using Klacks.Api.Domain.Services.ShiftSchedule;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Shifts;

public class GetShiftsByIdsQueryHandler : IRequestHandler<GetShiftsByIdsQuery, List<ShiftResource>>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftGroupFilterService _shiftGroupFilterService;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly ILogger<GetShiftsByIdsQueryHandler> _logger;

    public GetShiftsByIdsQueryHandler(
        IShiftRepository shiftRepository,
        IShiftGroupFilterService shiftGroupFilterService,
        ScheduleMapper scheduleMapper,
        ILogger<GetShiftsByIdsQueryHandler> logger)
    {
        _shiftRepository = shiftRepository;
        _shiftGroupFilterService = shiftGroupFilterService;
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

        var visibleGroupIds = await _shiftGroupFilterService.ResolveShiftListGroupIdsAsync(null);
        var query = _shiftRepository
            .GetQueryWithClient()
            .Where(s => distinctIds.Contains(s.Id)
                && s.AnalyseToken == null
                && s.ScenarioSourceShiftId == null);

        var shifts = await ShiftGroupVisibilityFilter.Apply(query, visibleGroupIds)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Bulk-loaded {Count} shifts for {Requested} requested IDs", shifts.Count, distinctIds.Count);

        return shifts.Select(_scheduleMapper.ToShiftResource).ToList();
    }
}
