// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

﻿using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Services.Shifts;
using Klacks.Api.Domain.Services.ShiftSchedule;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Shifts;

/// <summary>
/// Handler for the cut list of an order. Cuts of groups the caller may not see are dropped, exactly as if they
/// did not exist; group-less cuts stay visible to everyone.
/// </summary>
/// <param name="shiftRepository">Loads the cuts with their group items</param>
/// <param name="shiftGroupFilterService">Resolves the caller's group restriction (null = unrestricted)</param>
/// <param name="scheduleMapper">Maps the cuts to the API resource</param>
/// <param name="logger">Diagnostics logging</param>
public class CutListQueryhandler : IRequestHandler<CutListQuery, IEnumerable<ShiftResource>>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftGroupFilterService _shiftGroupFilterService;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly ILogger<CutListQueryhandler> _logger;

    public CutListQueryhandler(
        IShiftRepository shiftRepository,
        IShiftGroupFilterService shiftGroupFilterService,
        ScheduleMapper scheduleMapper,
        ILogger<CutListQueryhandler> logger)
    {
        _shiftRepository = shiftRepository;
        _shiftGroupFilterService = shiftGroupFilterService;
        _scheduleMapper = scheduleMapper;
        _logger = logger;
    }

    public async Task<IEnumerable<ShiftResource>> Handle(CutListQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching cut shift list for ID: {Id}", request.Id);
        
        try
        {
            var cuts = await _shiftRepository.CutList(request.Id);
            var visibleGroupIds = await _shiftGroupFilterService.ResolveShiftListGroupIdsAsync(null);
            var cutsList = cuts.Where(cut => ShiftGroupVisibilityFilter.IsVisible(cut, visibleGroupIds)).ToList();

            _logger.LogInformation("Retrieved {Count} cut shifts for ID: {Id}", cutsList.Count, request.Id);

            return cutsList.Select(s => _scheduleMapper.ToShiftResource(s)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching cut shift list for ID: {Id}", request.Id);
            throw new InvalidRequestException($"Failed to retrieve cut shift list for ID {request.Id}: {ex.Message}");
        }
    }
}
