using Klacks.Api.Interfaces;
using Klacks.Api.Interfaces.Domains;
using Klacks.Api.Models.Schedules;
using Klacks.Api.Resources.Filter;

namespace Klacks.Api.Services.Shifts;

public class ShiftFilterService : IShiftFilterService
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IDateRangeFilterService _dateRangeFilterService;
    private readonly IShiftSearchService _searchService;
    private readonly IShiftSortingService _sortingService;
    private readonly IShiftStatusFilterService _statusFilterService;

    public ShiftFilterService(
        IShiftRepository shiftRepository,
        IDateRangeFilterService dateRangeFilterService,
        IShiftSearchService searchService,
        IShiftSortingService sortingService,
        IShiftStatusFilterService statusFilterService)
    {
        _shiftRepository = shiftRepository;
        _dateRangeFilterService = dateRangeFilterService;
        _searchService = searchService;
        _sortingService = sortingService;
        _statusFilterService = statusFilterService;
    }

    public IQueryable<Shift> ApplyAllFilters(IQueryable<Shift> query, ShiftFilter filter)
    {
        query = _statusFilterService.ApplyStatusFilter(query, filter.IsOriginal);
        query = _dateRangeFilterService.ApplyDateRangeFilter(query, filter.ActiveDateRange, filter.FormerDateRange, filter.FutureDateRange);
        query = _searchService.ApplySearchFilter(query, filter.SearchString, filter.IncludeClientName);
        query = _sortingService.ApplySorting(query, filter.OrderBy, filter.SortOrder);

        return query;
    }

    public async Task<TruncatedShift> GetFilteredAndPaginatedShifts(ShiftFilter filter)
    {
        var baseQuery = filter.IncludeClientName 
            ? _shiftRepository.GetQueryWithClient() 
            : _shiftRepository.GetQuery();

        var filteredQuery = ApplyAllFilters(baseQuery, filter);

         return await _shiftRepository.GetPaginatedShifts(filteredQuery, filter);
    }
}