using Klacks.Api.Datas;
using Klacks.Api.Enums;
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

    public ShiftRepository(DataBaseContext context)
        : base(context)
    {
        this.context = context;
    }

    public new async Task<Shift?> Get(Guid id)
    {
        var shift = await context.Shift
            .Where(x => x.Id == id)
            .Include(x => x.Client)
            .ThenInclude(x => x.Addresses)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (shift != null)
        {
            shift.Groups = await GetGroupsForShift(id);
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
        var status = original ? ShiftStatus.Original : ShiftStatus.IsCut;

        tmp = tmp.Where(co => co.Status == status);

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
        var existingIds = await context.GroupItem
        .Where(gi => gi.GroupId == shiftId)
        .Select(x => x.Id)
        .ToListAsync();

        var newGroupIds = actualGroupIds.Where(x => !existingIds.Contains(x)).ToArray();
        var deleteGroupItems = await context.GroupItem.Where(gi => gi.GroupId == shiftId && !actualGroupIds.Contains(gi.GroupId)).ToArrayAsync();
        var newGroupItems = newGroupIds.Select(x => new GroupItem { ShiftId = shiftId, GroupId = x }).ToArray();

        context.GroupItem.RemoveRange(deleteGroupItems);
        context.GroupItem.AddRange(newGroupItems);
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
}