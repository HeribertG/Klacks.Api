// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Facade for the shift query pipeline. Delegates to specialized filter, search, sorting and pagination services.
/// </summary>
/// <param name="dateRangeFilterService">Filters shifts by date range (active, past, future)</param>
/// <param name="searchService">Filters shifts by search term</param>
/// <param name="sortingService">Sortiert Shifts nach Spalte und Richtung</param>
/// <param name="statusFilterService">Filtert Shifts nach Status (Original, SealedOrder etc.)</param>
/// <param name="paginationService">Paginiert die gefilterte Query</param>
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public class ShiftQueryPipelineService : IShiftQueryPipelineService
{
    private readonly IDateRangeFilterService _dateRangeFilterService;
    private readonly IShiftSearchService _searchService;
    private readonly IShiftSortingService _sortingService;
    private readonly IShiftStatusFilterService _statusFilterService;
    private readonly IShiftPaginationService _paginationService;

    public ShiftQueryPipelineService(
        IDateRangeFilterService dateRangeFilterService,
        IShiftSearchService searchService,
        IShiftSortingService sortingService,
        IShiftStatusFilterService statusFilterService,
        IShiftPaginationService paginationService)
    {
        _dateRangeFilterService = dateRangeFilterService;
        _searchService = searchService;
        _sortingService = sortingService;
        _statusFilterService = statusFilterService;
        _paginationService = paginationService;
    }

    public IQueryable<Shift> ApplyStatusFilter(IQueryable<Shift> query, ShiftFilterType filterType, bool isSealedOrder = false, bool isTimeRange = true, bool isSporadic = true)
    {
        return _statusFilterService.ApplyStatusFilter(query, filterType, isSealedOrder, isTimeRange, isSporadic);
    }

    public IQueryable<Shift> ApplyDateRangeFilter(IQueryable<Shift> query, bool activeDateRange, bool formerDateRange, bool futureDateRange)
    {
        return _dateRangeFilterService.ApplyDateRangeFilter(query, activeDateRange, formerDateRange, futureDateRange);
    }

    public IQueryable<Shift> ApplySearchFilter(IQueryable<Shift> query, string searchString, bool includeClient)
    {
        return _searchService.ApplySearchFilter(query, searchString, includeClient);
    }

    public IQueryable<Shift> ApplySorting(IQueryable<Shift> query, string orderBy, string sortOrder)
    {
        return _sortingService.ApplySorting(query, orderBy, sortOrder);
    }

    public async Task<TruncatedShift> ApplyPaginationAsync(IQueryable<Shift> filteredQuery, ShiftFilter filter)
    {
        return await _paginationService.ApplyPaginationAsync(filteredQuery, filter);
    }
}
