// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

﻿using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Domain.Services.Shifts;
using Klacks.Api.Domain.Services.ShiftSchedule;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Shifts;

/// <summary>
/// Handler for the paginated shift/order list. Restricts the list to the shifts the caller may see by group
/// (group-less shifts stay visible to everyone); a selected group the caller may not see yields only group-less shifts.
/// </summary>
/// <param name="shiftRepository">Filters and paginates the shifts</param>
/// <param name="shiftGroupFilterService">Resolves the caller's group restriction (null = unrestricted)</param>
/// <param name="scheduleMapper">Maps the result to the API resource</param>
/// <param name="logger">Diagnostics logging</param>
public class GetTruncatedListQueryHandler : IRequestHandler<GetTruncatedListQuery, TruncatedShiftResource>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftGroupFilterService _shiftGroupFilterService;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly ILogger<GetTruncatedListQueryHandler> _logger;

    public GetTruncatedListQueryHandler(
        IShiftRepository shiftRepository,
        IShiftGroupFilterService shiftGroupFilterService,
        ScheduleMapper scheduleMapper,
        ILogger<GetTruncatedListQueryHandler> logger)
    {
        _shiftRepository = shiftRepository;
        _shiftGroupFilterService = shiftGroupFilterService;
        _scheduleMapper = scheduleMapper;
        _logger = logger;
    }

    public async Task<TruncatedShiftResource> Handle(GetTruncatedListQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching truncated shift list");
        
        try
        {
            var selectedGroup = ShiftListViews.IsUnsealedDraftView(request.Filter) ? null : request.Filter.SelectedGroup;
            var visibleGroupIds = await _shiftGroupFilterService.ResolveShiftListGroupIdsAsync(selectedGroup);
            var truncatedShift = await _shiftRepository.GetFilteredAndPaginatedShifts(request.Filter, visibleGroupIds);
            
            _logger.LogInformation("Retrieved truncated shift list with {Count} shifts", truncatedShift.Shifts?.Count() ?? 0);
            
            return _scheduleMapper.ToTruncatedShiftResource(truncatedShift);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching truncated shift list");
            throw new InvalidRequestException($"Failed to retrieve truncated shift list: {ex.Message}");
        }
    }
}