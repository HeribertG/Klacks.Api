using Klacks.Api.Datas;
using Klacks.Api.Enums;
using Klacks.Api.Exceptions;
using Klacks.Api.Helper;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Associations;
using Klacks.Api.Models.Schedules;
using Klacks.Api.Resources.Filter;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Klacks.Api.Repositories;

public class ShiftRepository : BaseRepository<Shift>, IShiftRepository
{
    private readonly DataBaseContext context;

    public ShiftRepository(DataBaseContext context, ILogger<Shift> logger)
        : base(context, logger)
    {
        this.context = context;
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

    public async Task<List<Shift>> CutList(Guid id)
    {
        return await context.Shift.Where(x => x.OriginalId == id && x.Status == ShiftStatus.IsCut).AsNoTracking().ToListAsync();
    }

    public async Task<TruncatedShift> Truncated(ShiftFilter filter)
    {
        var tmp = FilterShift(filter);

        var count = tmp.Count();
        var maxPage = filter.NumberOfItemsPerPage > 0 ? (count / filter.NumberOfItemsPerPage) : 0;

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

        tmp = tmp.Skip(firstItem).Take(filter.NumberOfItemsPerPage);

        var shifts = count == 0 ? new List<Shift>() : await tmp.ToListAsync();
        var res = new TruncatedShift
        {
            Shifts = shifts,
            MaxItems = count,
        };

        if (filter.NumberOfItemsPerPage > 0)
        {
            res.MaxPages = count % filter.NumberOfItemsPerPage == 0 ? maxPage - 1 : maxPage;
        }

        res.CurrentPage = filter.RequiredPage;
        res.FirstItemOnPage = res.MaxItems <= firstItem ? -1 : firstItem;

        return res;
    }

    public IQueryable<Shift> FilterShift(ShiftFilter filter)
    {
        var tmp = context.Shift.AsNoTracking()
                               .AsQueryable();

        if (filter.IncludeClientName)
        {
            tmp = tmp.Include(x => x.Client).ThenInclude(x => x.Addresses!.Where(a => a != null));
        }

        tmp = FilterByStatus(filter.IsOriginal, tmp);
        tmp = FilterByDateRange(filter.ActiveDateRange, filter.FormerDateRange, filter.FutureDateRange, tmp);
        tmp = FilterBySearchString(filter.SearchString, filter.IncludeClientName, tmp);
        tmp = Sort(filter.OrderBy, filter.SortOrder, tmp);

        return tmp;
    }

    private IQueryable<Shift> FilterByDateRange(bool activeDateRange, bool formerDateRange, bool futureDateRange, IQueryable<Shift> tmp)
    {
        if (activeDateRange && formerDateRange && futureDateRange)
        {
            // No need for filters
        }
        else if (!activeDateRange && !formerDateRange && !futureDateRange)
        {
            tmp = Enumerable.Empty<Shift>().AsQueryable();
        }
        else
        {
            var nowDate = DateTime.Now;
            var nowDateOnly = DateOnly.FromDateTime(nowDate);

            // only active
            if (activeDateRange && !formerDateRange && !futureDateRange)
            {
                tmp = tmp.Where(co =>
                                co.FromDate <= nowDateOnly &&
                                (co.UntilDate.HasValue == false ||
                                (co.UntilDate.HasValue && co.UntilDate.Value >= nowDateOnly)
                                ));
            }

            // only former
            if (!activeDateRange && formerDateRange && !futureDateRange)
            {
                tmp = tmp.Where(co =>
                               (co.UntilDate.HasValue && co.UntilDate.Value < nowDateOnly));
            }

            // only future
            if (!activeDateRange && !formerDateRange && futureDateRange)
            {
                tmp = tmp.Where(co =>
                               (co.FromDate > nowDateOnly));
            }

            // former + active
            if (activeDateRange && formerDateRange && !futureDateRange)
            {
                tmp = tmp.Where(co =>
                                co.FromDate <= nowDateOnly &&
                                (co.UntilDate.HasValue == false ||
                                (co.UntilDate.HasValue && co.UntilDate.Value > nowDateOnly) ||
                                (co.UntilDate.HasValue && co.UntilDate.Value < nowDateOnly)));
            }

            // active + future
            if (activeDateRange && !formerDateRange && futureDateRange)
            {
                tmp = tmp.Where(co =>
                                 ((co.FromDate <= nowDateOnly &&
                                 (co.UntilDate.HasValue == false ||
                                 (co.UntilDate.HasValue && co.UntilDate.Value > nowDateOnly))) ||
                                 (co.FromDate > nowDateOnly)
                                 ));
            }

            // former + future
            if (!activeDateRange && formerDateRange && futureDateRange)
            {
                tmp = tmp.Where(co =>
                              (co.UntilDate.HasValue && co.UntilDate.Value < nowDateOnly) ||
                              (co.FromDate > nowDateOnly));
            }
        }

        return tmp;
    }

    private IQueryable<Shift> FilterByStatus(bool original, IQueryable<Shift> tmp)
    {
        if (original)
        {
            tmp = tmp.Where(co => co.Status == ShiftStatus.Original);
        }
        else
        {
            tmp = tmp.Where(co => co.Status >= ShiftStatus.IsCutOriginal);
        }

        return tmp;
    }

    private IQueryable<Shift> FilterBySearchString(string searchString, bool includeClient, IQueryable<Shift> tmp)
    {
        if (searchString == null || searchString.Length == 0)
        {
            return tmp;
        }

        var keywordList = searchString.TrimEnd().TrimStart().ToLower().Split(' ');

        if (keywordList.Length == 1)
        {
            if (keywordList[0].Length == 1)
            {
                return this.FilterBySearchStringFirstSymbol(keywordList[0], tmp);
            }
        }

        return this.FilterBySearchStringStandard(keywordList, includeClient, tmp);
    }

    private IQueryable<Shift> FilterBySearchStringFirstSymbol(string keyword, IQueryable<Shift> tmp)
    {
        tmp = tmp.Where(co =>
            co.Name.ToLower().Substring(0, 1) == keyword.ToLower());

        return tmp;
    }

    private IQueryable<Shift> FilterBySearchStringStandard(string[] keywordList, bool includeClient, IQueryable<Shift> tmp)
    {
        var normalizedKeywords = keywordList
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim().ToLower())
            .Distinct()
            .ToArray();

        if (normalizedKeywords.Length == 0)
        {
            return tmp;
        }

        var predicate = PredicateBuilder.False<Shift>();

        foreach (var keyword in normalizedKeywords)
        {
            predicate = predicate.Or(CreateShiftSearchPredicate(keyword));

            if (includeClient)
            {
                predicate = predicate.Or(CreateClientSearchPredicate(keyword));
            }
        }

        return tmp.Where(predicate);
    }

    private static Expression<Func<Shift, bool>> CreateShiftSearchPredicate(string keyword)
    {
        return shift =>
            EF.Functions.Like(shift.Name, $"%{keyword}%") ||
            EF.Functions.Like(shift.Abbreviation, keyword);
    }

    private static Expression<Func<Shift, bool>> CreateClientSearchPredicate(string keyword)
    {
        var pattern = $"%{keyword}%";

        return shift => shift.Client != null && (
            EF.Functions.Like(shift.Client.FirstName, pattern) ||
            EF.Functions.Like(shift.Client.SecondName, pattern) ||
            EF.Functions.Like(shift.Client.Name, pattern) ||
            EF.Functions.Like(shift.Client.MaidenName, pattern) ||
            EF.Functions.Like(shift.Client.Company, pattern)
        );
    }

    private IQueryable<Shift> Sort(string orderBy, string sortOrder, IQueryable<Shift> tmp)
    {
        if (sortOrder != string.Empty)
        {
            if (orderBy == "name")
            {
                return sortOrder == "asc" ? tmp.OrderBy(x => x.Name) : tmp.OrderByDescending(x => x.Name);
            }
            else if (orderBy == "description")
            {
                return sortOrder == "asc" ? tmp.OrderBy(x => x.Description) : tmp.OrderByDescending(x => x.Description);
            }
            else if (orderBy == "valid_from")
            {
                return sortOrder == "asc" ? tmp.OrderBy(x => x.FromDate) : tmp.OrderByDescending(x => x.FromDate);
            }
            else if (orderBy == "valid_until")
            {
                return sortOrder == "asc" ? tmp.OrderBy(x => x.UntilDate) : tmp.OrderByDescending(x => x.UntilDate);
            }
        }

        return tmp;
    }

    public async Task UpdateGroupItems(Guid shiftId, List<Guid> actualGroupIds)
    {
        Logger.LogInformation("Updating group items for shift ID: {ShiftId}", shiftId);
        try
        {
            var existingIds = await context.GroupItem
            .Where(gi => gi.GroupId == shiftId)
            .Select(x => x.Id)
            .ToListAsync();

            var newGroupIds = actualGroupIds.Where(x => !existingIds.Contains(x)).ToArray();
            var deleteGroupItems = await context.GroupItem.Where(gi => gi.GroupId == shiftId && !actualGroupIds.Contains(gi.GroupId)).ToArrayAsync();
            var newGroupItems = newGroupIds.Select(x => new GroupItem { ShiftId = shiftId, GroupId = x }).ToArray();

            context.GroupItem.RemoveRange(deleteGroupItems);
            context.GroupItem.AddRange(newGroupItems);

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

    private async Task<List<Group>> GetGroupsForShift(Guid shiftId)
    {
        var groups = await context.Group
            .Include(g => g.GroupItems.Where(gi => gi.ShiftId == shiftId))
            .Where(g => g.GroupItems.Any(gi => gi.ShiftId == shiftId))
            .AsNoTracking()
            .ToListAsync();

        return groups;
    }

    /// <summary>
    /// Adds a new Original Shift with proper Nested Set Model values
    /// </summary>
    public async Task<Shift> AddOriginalShift(Shift newShift)
    {
        Logger.LogInformation("Adding new original shift: {ShiftName}", newShift.Name);
        try
        {
            // Find the maximum rgt value among all root shifts (status: Original, no OriginalId)
            var maxRgt = await context.Shift
                .Where(s => s.Status == ShiftStatus.Original && s.OriginalId == null)
                .OrderByDescending(s => s.Rgt)
                .Select(s => (int?)s.Rgt)
                .FirstOrDefaultAsync() ?? 0;

            // Set Nested Set values for the new original shift
            newShift.Lft = maxRgt + 1;
            newShift.Rgt = maxRgt + 2;
            newShift.ParentId = null;
            newShift.RootId = null;  // Will be set to own ID after creation
            // OriginalId nur bei Original Status auf null setzen
            if (newShift.Status == ShiftStatus.Original)
            {
                newShift.OriginalId = null;
            }

            context.Shift.Add(newShift);
            
            // After saving, set RootId to own ID
            await context.SaveChangesAsync();
            newShift.RootId = newShift.Id;
            
            await context.SaveChangesAsync();
            
            Logger.LogInformation("Original shift {ShiftName} added successfully with lft={Lft}, rgt={Rgt}.", newShift.Name, newShift.Lft, newShift.Rgt);
            return newShift;
        }
        catch (DbUpdateException ex)
        {
            Logger.LogError(ex, "Failed to add original shift {ShiftName}. Database update error.", newShift.Name);
            throw new InvalidRequestException($"Failed to add original shift {newShift.Name} due to a database error.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An unexpected error occurred while adding original shift {ShiftName}.", newShift.Name);
            throw;
        }
    }

    /// <summary>
    /// Adds a new Cut Shift as child of an Original Shift with proper Nested Set Model values
    /// </summary>
    public async Task<Shift> AddCutShift(Shift newCutShift, Guid originalShiftId)
    {
        Logger.LogInformation("Adding cut shift to original shift {OriginalShiftId} for new shift {ShiftName}", originalShiftId, newCutShift.Name);
        
        var originalShift = await context.Shift
            .Where(s => s.Id == originalShiftId)
            .FirstOrDefaultAsync();

        if (originalShift == null)
        {
            Logger.LogWarning("Original shift with ID {OriginalShiftId} not found.", originalShiftId);
            throw new KeyNotFoundException($"Original shift with ID {originalShiftId} not found");
        }

        try
        {
            // Update all shifts that are affected by the insertion
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE \"shift\" SET \"rgt\" = \"rgt\" + 2 WHERE \"rgt\" >= @p0 AND (\"root_id\" = @p1 OR \"id\" = @p1) AND \"is_deleted\" = false",
                originalShift.Rgt, originalShift.RootId ?? originalShift.Id);

            await context.Database.ExecuteSqlRawAsync(
                "UPDATE \"shift\" SET \"lft\" = \"lft\" + 2 WHERE \"lft\" > @p0 AND (\"root_id\" = @p1 OR \"id\" = @p1) AND \"is_deleted\" = false",
                originalShift.Rgt, originalShift.RootId ?? originalShift.Id);

            // Set Nested Set values for the new cut shift
            newCutShift.Lft = originalShift.Rgt;
            newCutShift.Rgt = originalShift.Rgt + 1;
            newCutShift.ParentId = originalShift.Id;
            newCutShift.RootId = originalShift.RootId ?? originalShift.Id;
            newCutShift.OriginalId = originalShift.Id;
            newCutShift.Status = ShiftStatus.IsCut;

            context.Shift.Add(newCutShift);
            await context.SaveChangesAsync();
            Logger.LogInformation("Cut shift {ShiftName} added successfully to original shift {OriginalShiftId} with lft={Lft}, rgt={Rgt}.", newCutShift.Name, originalShiftId, newCutShift.Lft, newCutShift.Rgt);
            return newCutShift;
        }
        catch (DbUpdateException ex)
        {
            Logger.LogError(ex, "Failed to add cut shift {ShiftName} to original shift {OriginalShiftId}. Database update error.", newCutShift.Name, originalShiftId);
            throw new InvalidRequestException($"Failed to add cut shift {newCutShift.Name} to original shift {originalShiftId} due to a database error.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An unexpected error occurred while adding cut shift {ShiftName} to original shift {OriginalShiftId}.", newCutShift.Name, originalShiftId);
            throw;
        }
    }

    /// <summary>
    /// Override Add method to automatically handle Nested Set Model based on ShiftStatus
    /// </summary>
    public override async Task Add(Shift entity)
    {
        if (entity.Status == ShiftStatus.Original && entity.OriginalId == null)
        {
            // This is a new Original Shift (root shift)
            await AddOriginalShift(entity);
        }
        else if (entity.Status == ShiftStatus.IsCut && entity.OriginalId != null)
        {
            // This is a Cut Shift
            await AddCutShift(entity, entity.OriginalId.Value);
        }
        else if ((entity.Status == ShiftStatus.IsCutOriginal || entity.Status == ShiftStatus.ReadyToCut) && entity.OriginalId != null)
        {
            // This is a Cut-based Shift with OriginalId - use standard Add to preserve OriginalId
            await base.Add(entity);
        }
        else if ((entity.Status == ShiftStatus.IsCutOriginal || entity.Status == ShiftStatus.ReadyToCut) && entity.OriginalId == null)
        {
            // This is a root Cut-based Shift - treat as original for Nested Set
            await AddOriginalShift(entity);
        }
        else
        {
            // Für alle anderen Fälle, verwende die Standard Add Methode
            await base.Add(entity);
        }
    }
}