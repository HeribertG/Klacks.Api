using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Filter;

namespace Klacks.Api.Domain.Services.ShiftSchedule;

public class ShiftScheduleFilterService : IShiftScheduleFilterService
{
    private readonly IShiftScheduleSearchService _searchService;
    private readonly IShiftScheduleSortingService _sortingService;
    private readonly IShiftScheduleTypeFilterService _typeFilterService;

    public ShiftScheduleFilterService(
        IShiftScheduleSearchService searchService,
        IShiftScheduleSortingService sortingService,
        IShiftScheduleTypeFilterService typeFilterService)
    {
        _searchService = searchService;
        _sortingService = sortingService;
        _typeFilterService = typeFilterService;
    }

    public IQueryable<ShiftDayAssignment> ApplyAllFilters(IQueryable<ShiftDayAssignment> query, ShiftScheduleFilter filter)
    {
        query = _typeFilterService.ApplyTypeFilter(
            query,
            filter.IsSporadic,
            filter.IsTimeRange,
            filter.Container,
            filter.IsStandartShift);

        query = _searchService.ApplySearchFilter(query, filter.SearchString);

        query = _sortingService.ApplySorting(query, filter.OrderBy, filter.SortOrder);

        return query;
    }
}
