using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Filter;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories;

public class ShiftRepository : BaseRepository<Shift>, IShiftRepository
{
    private readonly DataBaseContext context;
    private readonly IDateRangeFilterService _dateRangeFilterService;
    private readonly IShiftSearchService _searchService;
    private readonly IShiftSortingService _sortingService;
    private readonly IShiftStatusFilterService _statusFilterService;
    private readonly IShiftPaginationService _paginationService;
    private readonly IShiftGroupManagementService _groupManagementService;

    public ShiftRepository(DataBaseContext context, ILogger<Shift> logger,
        IDateRangeFilterService dateRangeFilterService,
        IShiftSearchService searchService,
        IShiftSortingService sortingService,
        IShiftStatusFilterService statusFilterService,
        IShiftPaginationService paginationService,
        IShiftGroupManagementService groupManagementService)
        : base(context, logger)
    {
        this.context = context;
        _dateRangeFilterService = dateRangeFilterService;
        _searchService = searchService;
        _sortingService = sortingService;
        _statusFilterService = statusFilterService;
        _paginationService = paginationService;
        _groupManagementService = groupManagementService;
    }

    public new async Task<Shift?> Get(Guid id)
    {
        Logger.LogInformation("Fetching shift with ID: {ShiftId}", id);
        var shift = await context.Shift
            .Where(x => x.Id == id)
            .Include(x => x.Client)
            .ThenInclude(x => x.Addresses)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (shift != null)
        {
            shift.Groups = await _groupManagementService.GetGroupsForShiftAsync(id);
            Logger.LogInformation("Shift with ID: {ShiftId} found.", id);
        }
        else
        {
            Logger.LogWarning("Shift with ID: {ShiftId} not found.", id);
        }

        return shift;
    }

    public IQueryable<Shift> GetQuery()
    {
        return context.Shift
            .OrderBy(x => x.OriginalId)
            .ThenBy(x => x.FromDate)
            .ThenBy(x => x.StartShift)
            .AsNoTracking();
    }

    public IQueryable<Shift> GetQueryWithClient()
    {
        return context.Shift
            .Include(x => x.Client)
            .ThenInclude(x => x.Addresses!.Where(a => a != null))
            .OrderBy(x => x.OriginalId)
            .ThenBy(x => x.FromDate)
            .ThenBy(x => x.StartShift)
            .AsNoTracking();
    }

    public async Task<List<Shift>> CutList(Guid id)
    {
        return await context.Shift
            .Where(x => x.OriginalId == id && x.Status >= ShiftStatus.IsCutOriginal)
            .OrderBy(x => x.Lft)
            .ThenBy(x => x.FromDate)
            .ThenBy(x => x.StartShift)
            .AsNoTracking()
            .ToListAsync();
    }

    public IQueryable<Shift> FilterShifts(ShiftFilter filter)
    {
        Logger.LogInformation("Applying filters to shifts query");
        
        var baseQuery = filter.IncludeClientName 
            ? GetQueryWithClient() 
            : GetQuery();

        var query = _statusFilterService.ApplyStatusFilter(baseQuery, filter.IsOriginal);
        query = _dateRangeFilterService.ApplyDateRangeFilter(query, filter.ActiveDateRange, filter.FormerDateRange, filter.FutureDateRange);
        query = _searchService.ApplySearchFilter(query, filter.SearchString, filter.IncludeClientName);
        query = _sortingService.ApplySorting(query, filter.OrderBy, filter.SortOrder);

        Logger.LogInformation("Filters applied to shifts query");
        return query;
    }

    public async Task<TruncatedShift> GetFilteredAndPaginatedShifts(ShiftFilter filter)
    {
        Logger.LogInformation("Getting filtered and paginated shifts");
        var filteredQuery = FilterShifts(filter);
        return await _paginationService.ApplyPaginationAsync(filteredQuery, filter);
    }

    public async Task<TruncatedShift> GetPaginatedShifts(IQueryable<Shift> filteredQuery, ShiftFilter filter)
    {
        return await _paginationService.ApplyPaginationAsync(filteredQuery, filter);
    }

    public async Task UpdateGroupItems(Guid shiftId, List<Guid> actualGroupIds)
    {
        await _groupManagementService.UpdateGroupItemsAsync(shiftId, actualGroupIds);
    }

    public async Task<List<Group>> GetGroupsForShift(Guid shiftId)
    {
        return await _groupManagementService.GetGroupsForShiftAsync(shiftId);
    }
}
