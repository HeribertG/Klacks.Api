using DocumentFormat.OpenXml.Office2010.Excel;
using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Associations;
using Klacks.Api.Resources.Filter;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Repositories
{
  public class GroupRepository : BaseRepository<Group>, IGroupRepository
  {
    private readonly DataBaseContext context;

    public GroupRepository(DataBaseContext context)
       : base(context)
    {
      this.context = context;
    }

    public IQueryable<Group> FilterGroup(GroupFilter filter)
    {
      var tmp = context.Group.Include(gr => gr.GroupItems)
                             .ThenInclude(gi => gi.Client)
                             .AsNoTracking()
                             .AsQueryable();

      tmp = FilterByDateRange(filter.ActiveDateRange, filter.FormerDateRange, filter.FutureDateRange, tmp);
      tmp = FilterBySearchString(filter.SearchString, tmp);
      tmp = Sort(filter.OrderBy, filter.SortOrder, tmp);

      return tmp;
    }

    public new async Task<Group?> Get(Guid id)
    {
      return await context.Group.Where(x => x.Id == id).Include(x => x.GroupItems).ThenInclude(x => x.Client).AsNoTracking().FirstOrDefaultAsync();
    }

    public new void Put(Group model)
    {
      var existingIds = context.GroupItem.Where(x => x.GroupId == model.Id).Select(x => x.ClientId).ToList();
      var modelListIds = model.GroupItems.Select(x => x.ClientId).ToList();

      var newIds = modelListIds.Where(x => !existingIds.Contains(x)).ToList();
      var deleteItems = existingIds.Where(x => !modelListIds.Contains(x)).ToList();

      if (newIds.Any())
      {
        var lst = CreateList(newIds, model.Id);
        context.GroupItem.AddRange(lst.ToArray());
      }

      foreach (var id in deleteItems)
      {
        var item = context.GroupItem.FirstOrDefault(x => x.ClientId == id);
        if (item != null)
        {
          context.GroupItem.Remove(item);
        }
      }

      var group = context.Group.FirstOrDefault(x => x.Id == model.Id);
      if (group != null)
      {
        group.ValidFrom = model.ValidFrom;
        group.ValidUntil = model.ValidUntil;
        group.Name = model.Name;
        group.Description = model.Description;
        context.Group.Update(group);
      }
    }

    public async Task<TruncatedGroup> Truncated(GroupFilter filter)
    {
      var tmp = FilterGroup(filter);

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

      var groups = count == 0 ? new List<Group>() : await tmp.ToListAsync();
      var res = new TruncatedGroup
      {
        Groups = groups,
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

    private List<GroupItem> CreateList(List<Guid> list, Guid groubId)
    {
      var lst = new List<GroupItem>();
      foreach (var id in list)
      {
        lst.Add(new GroupItem() { ClientId = id, GroupId = groubId });
      }

      return lst;
    }

    private IQueryable<Group> FilterByDateRange(bool activeDateRange, bool formerDateRange, bool futureDateRange, IQueryable<Group> tmp)
    {
      if (activeDateRange && formerDateRange && futureDateRange)
      {
        // No need for filters
      }
      else if (!activeDateRange && !formerDateRange && !futureDateRange)
      {
        tmp = Enumerable.Empty<Group>().AsQueryable();
      }
      else
      {
        var nowDate = DateTime.Now;

        // only active
        if (activeDateRange && !formerDateRange && !futureDateRange)
        {
          tmp = tmp.Where(co =>
                          co.ValidFrom.Date <= nowDate &&
                          (co.ValidUntil.HasValue == false ||
                          (co.ValidUntil.HasValue && co.ValidUntil.Value.Date >= nowDate)
                          ));
        }

        // only former
        if (!activeDateRange && formerDateRange && !futureDateRange)
        {
          tmp = tmp.Where(co =>
                         (co.ValidUntil.HasValue && co.ValidUntil.Value.Date < nowDate));
        }

        // only future
        if (!activeDateRange && !formerDateRange && futureDateRange)
        {
          tmp = tmp.Where(co =>
                         (co.ValidFrom.Date > nowDate));
        }

        // former + active
        if (activeDateRange && formerDateRange && !futureDateRange)
        {
          tmp = tmp.Where(co =>
                          co.ValidFrom.Date <= nowDate &&
                          (co.ValidUntil.HasValue == false ||
                          (co.ValidUntil.HasValue && co.ValidUntil.Value.Date > nowDate) ||
                          (co.ValidUntil.HasValue && co.ValidUntil.Value.Date < nowDate)));
        }

        // active + future
        if (activeDateRange && !formerDateRange && futureDateRange)
        {
          tmp = tmp.Where(co =>
                           ((co.ValidFrom.Date <= nowDate &&
                           (co.ValidUntil.HasValue == false ||
                           (co.ValidUntil.HasValue && co.ValidUntil.Value.Date > nowDate))) ||
                           (co.ValidFrom.Date > nowDate)
                           ));
        }

        // former + future
        if (!activeDateRange && formerDateRange && futureDateRange)
        {
          tmp = tmp.Where(co =>
                        (co.ValidUntil.HasValue && co.ValidUntil.Value.Date < nowDate) ||
                        (co.ValidFrom.Date > nowDate));
        }
      }

      return tmp;
    }

    private IQueryable<Group> FilterBySearchString(string searchString, IQueryable<Group> tmp)
    {
      var keywordList = searchString.TrimEnd().TrimStart().ToLower().Split(' ');

      if (keywordList.Length == 1)
      {
        if (keywordList[0].Length == 1)
        {
          tmp = this.FilterBySearchStringFirstSymbol(keywordList[0], tmp);
        }
      }
      else
      {
        tmp = this.FilterBySearchStringStandart(keywordList, tmp);
      }

      return tmp;
    }

    private IQueryable<Group> FilterBySearchStringFirstSymbol(string keyword, IQueryable<Group> tmp)
    {
      tmp = tmp.Where(co =>
          co.Name.ToLower().Substring(0, 1) == keyword.ToLower());

      return tmp;
    }

    private IQueryable<Group> FilterBySearchStringStandart(string[] keywordList, IQueryable<Group> tmp)
    {
      var list = new List<Guid>();
      foreach (var keyword in keywordList)
      {
        foreach (var item in tmp)
        {
          var tmpWord = keyword.TrimEnd().TrimStart().ToLower();

          if (item.Name != null && item.Name.ToLower().Contains(tmpWord))
          {
            list.Add(item.Id);
          }
        }
      }

      tmp = tmp.Where(x => list.Contains(x.Id));
      return tmp;
    }

    private IQueryable<Group> Sort(string orderBy, string sortOrder, IQueryable<Group> tmp)
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
          return sortOrder == "asc" ? tmp.OrderBy(x => x.ValidFrom) : tmp.OrderByDescending(x => x.ValidFrom);
        }
        else if (orderBy == "valid_until")
        {
          return sortOrder == "asc" ? tmp.OrderBy(x => x.ValidUntil) : tmp.OrderByDescending(x => x.ValidUntil);
        }
      }

      return tmp;
    }
  }
}
