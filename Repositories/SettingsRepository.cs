using Klacks.Api.Datas;
using Klacks.Api.Helper.Excel;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Associations;
using Klacks.Api.Models.Settings;
using Klacks.Api.Resources;
using Klacks.Api.Resources.Filter;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Repositories
{
  public class SettingsRepository : ISettingsRepository
  {
    private readonly DataBaseContext context;

    public SettingsRepository(DataBaseContext context)
    {
      this.context = context;
    }

    #region Setting

    public async Task<Models.Settings.Settings> AddSetting(Models.Settings.Settings settings)
    {
      await context.Settings.AddAsync(settings);
      return settings;
    }

    public async Task<Models.Settings.Settings?> GetSetting(string type)
    {
      return await context.Settings.FirstOrDefaultAsync(x => x.Type == type);
    }

    public async Task<IEnumerable<Models.Settings.Settings>> GetSettingsList()
    {
      return await context.Settings.ToListAsync();
    }

    public Task<Models.Settings.Settings> PutSetting(Models.Settings.Settings settings)
    {
      context.Settings.Update(settings);
      return Task.FromResult(settings);
    }

    #endregion Setting

    #region Macro

    public Macro AddMacro(Macro macro)
    {
      context.Macro.Add(macro);
      return macro;
    }

    public async Task<Macro> DeleteMacro(Guid id)
    {
      var macro = await context.Macro.FindAsync(id);

      context.Macro.Remove(macro!);

      return macro!;
    }

    public async Task<Macro> GetMacro(Guid id)
    {
      var macro = await context.Macro.FindAsync(id);

      return macro!;
    }

    public async Task<List<Macro>> GetMacroList()
    {
      return await context.Macro.ToListAsync();
    }

    public bool MacroExists(Guid id)
    {
      return context.Macro.Any(e => e.Id == id);
    }

    public Macro PutMacro(Macro macro)
    {
      context.Macro.Update(macro);
      return macro;
    }

    public void RemoveMacro(Macro macro)
    {
      context.Macro.Remove(macro);
    }

    #endregion Macro

    #region BankDetails

    public BankDetails AddBankDetail(BankDetails bankDetails)
    {
      context.BankDetails.Add(bankDetails);

      return bankDetails;
    }

    public bool BankDetailExists(Guid id)
    {
      return context.BankDetails.Any(e => e.Id == id);
    }

    public async Task<BankDetails> DeleteBankDetail(Guid id)
    {
      var bankDetails = await context.BankDetails.FindAsync(id);

      context.BankDetails.Remove(bankDetails!);

      return bankDetails!;
    }

    public async Task<BankDetails> GetBankDetail(Guid id)
    {
      var bankDetails = await context.BankDetails.FindAsync(id);

      return bankDetails!;
    }

    public async Task<List<BankDetails>> GetBankDetailList()
    {
      return await context.BankDetails.ToListAsync();
    }

    public BankDetails PutBankDetail(BankDetails bankDetails)
    {
      context.BankDetails.Update(bankDetails);

      return bankDetails;
    }

    public void RemoveBankDetail(BankDetails bankDetails)
    {
      context.BankDetails.Remove(bankDetails);
    }

    #endregion BankDetails

    #region MacroType

    public MacroType AddMacroType(MacroType macroType)
    {
      context.MacroType.Add(macroType);
      return macroType;
    }

    public async Task<MacroType> DeleteMacroType(Guid id)
    {
      var macroType = await context.MacroType.FindAsync(id);

      context.MacroType.Remove(macroType!);

      return macroType!;
    }

    public async Task<MacroType> GetMacroType(Guid id)
    {
      var macroType = await context.MacroType.FindAsync(id);

      return macroType!;
    }

    public async Task<List<MacroType>> GetOriginalMacroTypeList()
    {
      return await context.MacroType.ToListAsync();
    }

    public bool MacroTypeExists(Guid id)
    {
      return context.MacroType.Any(e => e.Id == id);
    }

    public MacroType PutMacroType(MacroType macroType)
    {
      context.MacroType.Update(macroType);
      return macroType;
    }

    public void RemoveMacroType(MacroType macroType)
    {
      context.MacroType.Remove(macroType);
    }

    #endregion MacroType

    #region Vat

    public Vat AddVAT(Vat vat)
    {
      context.Vat.Add(vat);

      return vat;
    }

    public async Task<Vat> DeleteVAT(Guid id)
    {
      var vat = await context.Vat.FindAsync(id);

      context.Vat.Remove(vat!);

      return vat!;
    }

    public async Task<Vat> GetVAT(Guid id)
    {
      var vat = await context.Vat.FindAsync(id);

      return vat!;
    }

    public async Task<List<Vat>> GetVATList()
    {
      return await context.Vat.ToListAsync();
    }

    public Vat PutVAT(Vat vat)
    {
      context.Vat.Update(vat);

      return vat;
    }

    public void RemoveVAT(Vat vat)
    {
      context.Vat.Remove(vat);
    }

    public bool VATExists(Guid id)
    {
      return context.Vat.Any(e => e.Id == id);
    }

    #endregion Vat

    #region CalendarRule

    public CalendarRule AddCalendarRule(CalendarRule calendarRule)
    {
      this.context.CalendarRule.Add(calendarRule);

      return calendarRule;
    }

    public bool CalendarRuleExists(Guid id)
    {
      return this.context.CalendarRule.Any(e => e.Id == id);
    }

    public HttpResultResource CreateExcelFile(CalendarRulesFilter filter)
    {
      var result = new HttpResultResource();
      try
      {
        var tmp = this.FilterCalendarRule(filter);
        var excel = new CalendarRulesExcel(tmp);
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

    public async Task<CalendarRule> DeleteCalendarRule(Guid id)
    {
      var calendarRule = await this.context.CalendarRule.FindAsync(id);

      this.context.CalendarRule.Remove(calendarRule!);

      return calendarRule!;
    }

    public async Task<CalendarRule> GetCalendarRule(Guid id)
    {
      var calendarRule = await this.context.CalendarRule.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

      return calendarRule!;
    }

    public async Task<List<CalendarRule>> GetCalendarRuleList()
    {
      return await this.context.CalendarRule.AsNoTracking().ToListAsync();
    }

    public async Task<TruncatedCalendarRule> GetTruncatedCalendarRuleList(CalendarRulesFilter filter)
    {
      var count = 0;

      var tmp = FilterCalendarRule(filter);

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

      var res = new TruncatedCalendarRule
      {
        CalendarRules = await tmp!.ToListAsync(),
        MaxItems = count,
      };

      res.MaxPages = filter.NumberOfItemsPerPage > 0 ? (res.MaxItems / filter.NumberOfItemsPerPage) : 0;
      res.CurrentPage = filter.RequiredPage;
      res.FirstItemOnPage = firstItem;

      return res;
    }

    public CalendarRule PutCalendarRule(CalendarRule calendarRule)
    {
      this.context.CalendarRule.Update(calendarRule);

      return calendarRule;
    }

    public void RemoveCalendarRule(CalendarRule calendarRule)
    {
      this.context.CalendarRule.Remove(calendarRule);
    }

    #endregion CalendarRule

    private IQueryable<CalendarRule> FilterByState(List<StateCountryToken> list, IQueryable<CalendarRule> tmp)
    {
      var filteredStateList = list.Where(x => x.Select == true).Select(x => x.State).ToList();
      var filteredCountryList = list.Where(x => x.Select == true).Select(x => x.Country).Distinct().ToList();
      filteredStateList.AddRange(filteredCountryList.ToArray());

      tmp = tmp.Where(st => filteredStateList.Contains(st.State));
      tmp = tmp.Where(co => filteredCountryList.Contains(co.Country));

      return tmp;
    }

    private IQueryable<CalendarRule> FilterCalendarRule(CalendarRulesFilter filter)
    {
      var tmp = context.CalendarRule.AsQueryable();

      tmp = FilterByState(filter.List, tmp);

      tmp = Sort(filter.OrderBy, filter.SortOrder, filter.Language, tmp);

      return tmp;
    }

    private IQueryable<CalendarRule> Sort(string orderBy, string sortOrder, string language, IQueryable<CalendarRule> tmp)
    {
      var lang = language.ToLower();
      if (sortOrder != string.Empty)
      {
        if (orderBy == "Name")
        {
          switch (lang)
          {
            case "de":
              return sortOrder == "asc" ? tmp.OrderBy(x => x.Name!.De!) : tmp.OrderByDescending(x => x.Name!.De!);

            case "en":
              return sortOrder == "asc" ? tmp.OrderBy(x => x.Name!.En!) : tmp.OrderByDescending(x => x.Name!.En!);

            case "fr":
              return sortOrder == "asc" ? tmp.OrderBy(x => x.Name!.Fr!) : tmp.OrderByDescending(x => x.Name!.Fr!);

            case "it":
              return sortOrder == "asc" ? tmp.OrderBy(x => x.Name!.It!) : tmp.OrderByDescending(x => x.Name!.It!);
          }
        }
        else if (orderBy == "state")
        {
          return sortOrder == "asc" ? tmp.OrderBy(x => x.State) : tmp.OrderByDescending(x => x.State);
        }
        else if (orderBy == "country")
        {
          return sortOrder == "asc" ? tmp.OrderBy(x => x.Country) : tmp.OrderByDescending(x => x.Country);
        }
        else if (orderBy == "description")
        {
          switch (lang)
          {
            case "de":
              return sortOrder == "asc" ? tmp.OrderBy(x => x.Description!.De!) : tmp.OrderByDescending(x => x.Description!.De!);

            case "en":
              return sortOrder == "asc" ? tmp.OrderBy(x => x.Description!.En!) : tmp.OrderByDescending(x => x.Description!.En!);

            case "fr":
              return sortOrder == "asc" ? tmp.OrderBy(x => x.Description!.Fr!) : tmp.OrderByDescending(x => x.Description!.Fr!);

            case "it":
              return sortOrder == "asc" ? tmp.OrderBy(x => x.Description!.It!) : tmp.OrderByDescending(x => x.Description!.It!);
          }
        }
      }

      return tmp;
    }
  }
}
