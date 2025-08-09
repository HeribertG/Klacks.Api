using Klacks.Api.Datas;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Models.Associations;
using Klacks.Api.Models.Schedules;
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

    public ShiftRepository(DataBaseContext context, ILogger<Shift> logger,
        IDateRangeFilterService dateRangeFilterService,
        IShiftSearchService searchService,
        IShiftSortingService sortingService,
        IShiftStatusFilterService statusFilterService)
        : base(context, logger)
    {
        this.context = context;
        _dateRangeFilterService = dateRangeFilterService;
        _searchService = searchService;
        _sortingService = sortingService;
        _statusFilterService = statusFilterService;
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
            shift.Groups = await GetGroupsForShift(id);
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
        return await GetPaginatedShifts(filteredQuery, filter);
    }

    public async Task<TruncatedShift> GetPaginatedShifts(IQueryable<Shift> filteredQuery, ShiftFilter filter)
    {
        var count = await filteredQuery.CountAsync();
        var maxPage = filter.NumberOfItemsPerPage > 0 ? (count / filter.NumberOfItemsPerPage) : 0;
        var firstItem = CalculateFirstItem(filter, count);

        var shifts = count == 0
            ? new List<Shift>()
            : await filteredQuery.Skip(firstItem).Take(filter.NumberOfItemsPerPage).ToListAsync();

        var result = new TruncatedShift
        {
            Shifts = shifts,
            MaxItems = count,
            CurrentPage = filter.RequiredPage,
            FirstItemOnPage = count <= firstItem ? -1 : firstItem
        };

        if (filter.NumberOfItemsPerPage > 0)
        {
            result.MaxPages = count % filter.NumberOfItemsPerPage == 0 ? maxPage - 1 : maxPage;
        }

        return result;
    }

    private int CalculateFirstItem(ShiftFilter filter, int count)
    {
        var firstItem = 0;

        if (count > 0 && count > filter.NumberOfItemsPerPage)
        {
            if ((filter.IsNextPage.HasValue || filter.IsPreviousPage.HasValue) && filter.FirstItemOnLastPage.HasValue)
            {
                if (filter.IsNextPage.HasValue)
                {
                    firstItem = filter.FirstItemOnLastPage.Value + filter.NumberOfItemsPerPage;
                }
                else
                {
                    var numberOfItem = filter.NumberOfItemOnPreviousPage ?? filter.NumberOfItemsPerPage;
                    firstItem = filter.FirstItemOnLastPage.Value - numberOfItem;
                    if (firstItem < 0)
                    {
                        firstItem = 0;
                    }
                }
            }
            else
            {
                firstItem = filter.RequiredPage * filter.NumberOfItemsPerPage;
            }
        }
        else
        {
            firstItem = filter.RequiredPage * filter.NumberOfItemsPerPage;
        }

        return firstItem;
    }

    public async Task UpdateGroupItems(Guid shiftId, List<Guid> actualGroupIds)
    {
        Logger.LogInformation("Updating group items for shift ID: {ShiftId}", shiftId);
        try
        {
            var existingIds = await context.GroupItem
                .Where(gi => gi.ShiftId == shiftId)
                .Select(x => x.GroupId)
                .ToListAsync();

            var newGroupIds = actualGroupIds.Where(x => !existingIds.Contains(x)).ToArray();
            var deleteGroupItems = await context.GroupItem
                .Where(gi => gi.ShiftId == shiftId && !actualGroupIds.Contains(gi.GroupId))
                .ToArrayAsync();
            var newGroupItems = newGroupIds.Select(x => new GroupItem { ShiftId = shiftId, GroupId = x }).ToArray();

            if (deleteGroupItems.Any())
            {
                context.GroupItem.RemoveRange(deleteGroupItems);
            }

            if (newGroupItems.Any())
            {
                context.GroupItem.AddRange(newGroupItems);
            }

            await context.SaveChangesAsync();
            Logger.LogInformation("Group items for shift ID: {ShiftId} updated successfully.", shiftId);
        }
        catch (DbUpdateException ex)
        {
            Logger.LogError(ex, "Failed to update group items for shift ID: {ShiftId}.", shiftId);
            throw new DbUpdateException($"Failed to update group items for shift ID: {shiftId}.", ex);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An unexpected error occurred while updating group items for shift ID: {ShiftId}.", shiftId);
            throw;
        }
    }

    public async Task<List<Group>> GetGroupsForShift(Guid shiftId)
    {
        return await context.Group
            .Include(g => g.GroupItems.Where(gi => gi.ShiftId == shiftId))
            .Where(g => g.GroupItems.Any(gi => gi.ShiftId == shiftId))
            .AsNoTracking()
            .ToListAsync();
    }
}
