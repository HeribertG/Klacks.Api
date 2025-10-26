using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Filter;

namespace Klacks.Api.Domain.Services.Shifts;

public class ShiftFilterService : IShiftFilterService
{
    private readonly IDateRangeFilterService _dateRangeFilterService;
    private readonly IShiftSearchService _searchService;
    private readonly IShiftSortingService _sortingService;
    private readonly IShiftStatusFilterService _statusFilterService;

    public ShiftFilterService(
        IDateRangeFilterService dateRangeFilterService,
        IShiftSearchService searchService,
        IShiftSortingService sortingService,
        IShiftStatusFilterService statusFilterService)
    {
        _dateRangeFilterService = dateRangeFilterService;
        _searchService = searchService;
        _sortingService = sortingService;
        _statusFilterService = statusFilterService;
    }

    public IQueryable<Shift> ApplyAllFilters(IQueryable<Shift> query, ShiftFilter filter)
    {
        query = _statusFilterService.ApplyStatusFilter(query, filter.FilterType);
        query = _dateRangeFilterService.ApplyDateRangeFilter(query, filter.ActiveDateRange, filter.FormerDateRange, filter.FutureDateRange);
        query = _searchService.ApplySearchFilter(query, filter.SearchString, filter.IncludeClientName);
        query = _sortingService.ApplySorting(query, filter.OrderBy, filter.SortOrder);

        return query;
    }
}