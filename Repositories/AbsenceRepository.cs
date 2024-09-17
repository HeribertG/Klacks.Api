using Klacks_api.Datas;
using Klacks_api.Helper.Excel;
using Klacks_api.Interfaces;
using Klacks_api.Models.Schedules;
using Klacks_api.Resources;
using Klacks_api.Resources.Filter;
using Microsoft.EntityFrameworkCore;

namespace Klacks_api.Repositories
{
  public class AbsenceRepository : BaseRepository<Absence>, IAbsenceRepository
  {
    private readonly DataBaseContext context;

    public AbsenceRepository(DataBaseContext context)
        : base(context)
    {
      this.context = context;
    }

    public HttpResultResource CreateExcelFile(string language)
    {
      var result = new HttpResultResource();
      try
      {
        var tmp = this.context.Absence.OrderBy(x => x.Name.De).ThenBy(x => x.Description.De).AsQueryable();
        var excel = new AbsenceExcel(tmp, language);
        result.Success = true;
        result.Messages = excel.FileName;
      }
      catch (Exception e)
      {
        result.Success = false;
        result.Messages = e.Message;
      }

      return result;
    }

    public async Task<TruncatedAbsence_dto> Truncated(AbsenceFilter filter)
    {
      var count = 0;

      var tmp = this.context.Absence.AsQueryable();
      tmp = this.Sort(filter.OrderBy, filter.SortOrder, filter.Language, tmp);

      if (tmp != null)
      {
        count = tmp.Count();
      }

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

        tmp = tmp!.Skip(firstItem).Take(filter.NumberOfItemsPerPage);
      }

      var res = new TruncatedAbsence_dto
      {
        Absences = await tmp!.ToListAsync(),
        MaxItems = count,
      };

      var maxPage = filter.NumberOfItemsPerPage > 0 ? (res.MaxItems / filter.NumberOfItemsPerPage) : 0;

      res.MaxPages = count % filter.NumberOfItemsPerPage == 0 ? maxPage - 1 : maxPage;
      res.CurrentPage = filter.RequiredPage;
      res.FirstItemOnPage = firstItem;

      return res;
    }

    private IQueryable<Absence> Sort(string orderBy, string sortOrder, string language, IQueryable<Absence> tmp)
    {
      var lang = language.ToLower();
      if (sortOrder != string.Empty)
      {
        if (orderBy == "Name")
        {
          switch (lang)
          {
            case "de":
              return sortOrder == "asc" ? tmp.OrderBy(x => x.Name.De!) : tmp.OrderByDescending(x => x.Name.De!);

            case "en":
              return sortOrder == "asc" ? tmp.OrderBy(x => x.Name.En!) : tmp.OrderByDescending(x => x.Name.En!);

            case "fr":
              return sortOrder == "asc" ? tmp.OrderBy(x => x.Name.Fr!) : tmp.OrderByDescending(x => x.Name.Fr!);

            case "it":
              return sortOrder == "asc" ? tmp.OrderBy(x => x.Name.It!) : tmp.OrderByDescending(x => x.Name.It!);
          }
        }
        else if (orderBy == "description")
        {
          switch (lang)
          {
            case "de":
              return sortOrder == "asc" ? tmp.OrderBy(x => x.Description.De!) : tmp.OrderByDescending(x => x.Description.De!);

            case "en":
              return sortOrder == "asc" ? tmp.OrderBy(x => x.Description.En!) : tmp.OrderByDescending(x => x.Description.En!);

            case "fr":
              return sortOrder == "asc" ? tmp.OrderBy(x => x.Description.Fr!) : tmp.OrderByDescending(x => x.Description.Fr!);

            case "it":
              return sortOrder == "asc" ? tmp.OrderBy(x => x.Description.It!) : tmp.OrderByDescending(x => x.Description.It!);
          }
        }
      }

      return tmp;
    }
  }
}
