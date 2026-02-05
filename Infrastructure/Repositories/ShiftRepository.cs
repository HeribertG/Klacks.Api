using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.Services.Common;
using Klacks.Api.Application.DTOs.Filter;
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
    private readonly EntityCollectionUpdateService _collectionUpdateService;
    private readonly IShiftValidator _shiftValidator;
    private readonly ScheduleMapper _scheduleMapper;

    public ShiftRepository(DataBaseContext context, ILogger<Shift> logger,
        IDateRangeFilterService dateRangeFilterService,
        IShiftSearchService searchService,
        IShiftSortingService sortingService,
        IShiftStatusFilterService statusFilterService,
        IShiftPaginationService paginationService,
        IShiftGroupManagementService groupManagementService,
        EntityCollectionUpdateService collectionUpdateService,
        IShiftValidator shiftValidator,
        ScheduleMapper scheduleMapper)
        : base(context, logger)
    {
        this.context = context;
        _dateRangeFilterService = dateRangeFilterService;
        _searchService = searchService;
        _sortingService = sortingService;
        _statusFilterService = statusFilterService;
        _paginationService = paginationService;
        _groupManagementService = groupManagementService;
        _collectionUpdateService = collectionUpdateService;
        _shiftValidator = shiftValidator;
        _scheduleMapper = scheduleMapper;
    }

    public new async Task Add(Shift shift)
    {
        _shiftValidator.EnsureUniqueGroupItems(shift.GroupItems);

        foreach (var groupItem in shift.GroupItems)
        {
            groupItem.ShiftId = shift.Id;
        }

        await context.Shift.AddAsync(shift);
        Logger.LogInformation("Shift added: {ShiftId}, GroupItems count: {Count}", shift.Id, shift.GroupItems.Count);
    }

    public new async Task<Shift?> Put(Shift shift)
    {
        var existingShift = await context.Shift
            .Include(s => s.Client)
                .ThenInclude(c => c.Addresses)
            .Include(s => s.GroupItems)
                .ThenInclude(gi => gi.Group)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == shift.Id);

        if (existingShift == null)
        {
            Logger.LogWarning("Shift not found for update: {ShiftId}", shift.Id);
            return null;
        }

        var entry = context.Entry(existingShift);
        entry.CurrentValues.SetValues(shift);
        entry.State = EntityState.Modified;

        _collectionUpdateService.UpdateCollection(
            existingShift.GroupItems,
            shift.GroupItems,
            existingShift.Id,
            (groupItem, shiftId) => groupItem.ShiftId = shiftId);

        _shiftValidator.EnsureUniqueGroupItems(existingShift.GroupItems);

        Logger.LogInformation("Shift updated: {ShiftId}, GroupItems count: {Count}", shift.Id, existingShift.GroupItems.Count);
        return existingShift;
    }

    public new async Task<Shift?> Get(Guid id)
    {
        Logger.LogInformation("Fetching shift with ID: {ShiftId}", id);
        var shift = await context.Shift
            .Where(x => x.Id == id)
            .Include(x => x.Client!)
                .ThenInclude(x => x.Addresses!)
            .Include(x => x.GroupItems)
                .ThenInclude(gi => gi.Group)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (shift != null)
        {
            Logger.LogInformation("Shift with ID: {ShiftId} found with {GroupItemsCount} group items.", id, shift.GroupItems.Count);
        }
        else
        {
            Logger.LogWarning("Shift with ID: {ShiftId} not found.", id);
        }

        return shift;
    }

    public async Task<Shift?> GetTrackedOrFromDb(Guid id)
    {
        var trackedShift = context.Shift.Local.FirstOrDefault(s => s.Id == id);

        if (trackedShift != null)
        {
            Logger.LogInformation("Shift {ShiftId} found in EF Change Tracker (tracked entity)", id);
            return trackedShift;
        }

        Logger.LogInformation("Shift {ShiftId} not in Change Tracker, loading from database", id);
        return await Get(id);
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
            .Include(x => x.Client!)
            .ThenInclude(x => x.Addresses!.Where(a => a != null))
            .OrderBy(x => x.OriginalId)
            .ThenBy(x => x.FromDate)
            .ThenBy(x => x.StartShift)
            .AsNoTracking();
    }

    public async Task<List<Shift>> CutList(Guid id, DateOnly? filterClosedBefore = null, bool tracked = false)
    {
        var query = context.Shift
            .Where(x => x.OriginalId == id );

        if (filterClosedBefore.HasValue)
        {
            query = query.Where(x => !x.UntilDate.HasValue || x.UntilDate >= filterClosedBefore.Value);
        }

        query = query
            .Include(x => x.GroupItems)
                .ThenInclude(gi => gi.Group)
            .OrderBy(x => x.Lft)
            .ThenBy(x => x.FromDate)
            .ThenBy(x => x.StartShift)
            .AsSplitQuery();

        if (!tracked)
        {
            query = query.AsNoTracking();
        }

        return await query.ToListAsync();
    }

    public async Task<Shift?> GetSealedOrder(Guid originalId)
    {
        return await context.Shift
            .Where(x => x.Id == originalId && x.Status == ShiftStatus.SealedOrder)
            .Include(x => x.GroupItems)
                .ThenInclude(gi => gi.Group)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    public IQueryable<Shift> FilterShifts(ShiftFilter filter)
    {
        Logger.LogInformation("Applying filters to shifts query");

        var baseQuery = filter.IncludeClientName
            ? GetQueryWithClient()
            : GetQuery();

        bool shouldApplyGroupFilter = !(filter.FilterType == ShiftFilterType.Original && filter.IsSealedOrder == false);
        if (shouldApplyGroupFilter && filter.SelectedGroup.HasValue)
        {
            Logger.LogInformation("Including GroupItems for group filter: {GroupId}", filter.SelectedGroup.Value);
            baseQuery = baseQuery.Include(s => s.GroupItems);
        }

        var query = _statusFilterService.ApplyStatusFilter(baseQuery, filter.FilterType, filter.IsSealedOrder, filter.IsTimeRange, filter.IsSporadic);
        query = _dateRangeFilterService.ApplyDateRangeFilter(query, filter.ActiveDateRange, filter.FormerDateRange, filter.FutureDateRange);
        query = _searchService.ApplySearchFilter(query, filter.SearchString, filter.IncludeClientName);

        if (shouldApplyGroupFilter && filter.SelectedGroup.HasValue)
        {
            Logger.LogInformation("Applying group filter for group: {GroupId}", filter.SelectedGroup.Value);
            query = query.Where(s =>
                !s.GroupItems.Any() ||
                s.GroupItems.Any(gi => gi.GroupId == filter.SelectedGroup.Value));
        }

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

    public async Task<Shift> AddWithSealedOrderHandling(Shift shift)
    {
        await Add(shift);

        if (shift.Status != ShiftStatus.SealedOrder)
        {
            return shift;
        }

        Logger.LogInformation(
            "Creating OriginalShift from SealedOrder: SealedOrderId={SealedOrderId}",
            shift.Id);

        await _shiftValidator.EnsureNoOriginalShiftCopyExists(shift.Id, this);

        var originalShift = _scheduleMapper.CloneShift(shift);
        originalShift.Status = ShiftStatus.OriginalShift;
        originalShift.OriginalId = shift.Id;

        await Add(originalShift);

        Logger.LogInformation(
            "Created OriginalShift: NewShiftId={NewShiftId}, OriginalId={OriginalId}, Status={Status}",
            originalShift.Id, originalShift.OriginalId, originalShift.Status);

        return originalShift;
    }

    public async Task<Shift?> PutWithSealedOrderHandling(Shift shift)
    {
        var updatedShift = await Put(shift);

        if (updatedShift == null)
        {
            return null;
        }

        if (updatedShift.Status != ShiftStatus.SealedOrder)
        {
            return updatedShift;
        }

        Logger.LogInformation(
            "Creating OriginalShift from SealedOrder after update: SealedOrderId={SealedOrderId}",
            updatedShift.Id);

        await _shiftValidator.EnsureNoOriginalShiftCopyExists(updatedShift.Id, this);

        var originalShift = _scheduleMapper.CloneShift(updatedShift);
        originalShift.Status = ShiftStatus.OriginalShift;
        originalShift.OriginalId = updatedShift.Id;

        await Add(originalShift);

        Logger.LogInformation(
            "Created OriginalShift: NewShiftId={NewShiftId}, OriginalId={OriginalId}, Status={Status}",
            originalShift.Id, originalShift.OriginalId, originalShift.Status);

        return originalShift;
    }
}
