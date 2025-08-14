using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Presentation.DTOs.Filter;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly DataBaseContext context;
    private readonly ICalendarRuleFilterService _filterService;
    private readonly ICalendarRuleSortingService _sortingService;
    private readonly ICalendarRulePaginationService _paginationService;
    private readonly IMacroManagementService _macroManagementService;
    private readonly IMacroTypeManagementService _macroTypeManagementService;
    private readonly IVatManagementService _vatManagementService;

    public SettingsRepository(DataBaseContext context,
        ICalendarRuleFilterService filterService,
        ICalendarRuleSortingService sortingService,
        ICalendarRulePaginationService paginationService,
        IMacroManagementService macroManagementService,
        IMacroTypeManagementService macroTypeManagementService,
        IVatManagementService vatManagementService)
    {
        this.context = context;
        _filterService = filterService;
        _sortingService = sortingService;
        _paginationService = paginationService;
        _macroManagementService = macroManagementService;
        _macroTypeManagementService = macroTypeManagementService;
        _vatManagementService = vatManagementService;
    }

    #region Setting

    public async Task<Klacks.Api.Domain.Models.Settings.Settings> AddSetting(Klacks.Api.Domain.Models.Settings.Settings settings)
    {
        await context.Settings.AddAsync(settings);
        return settings;
    }

    public async Task<Klacks.Api.Domain.Models.Settings.Settings?> GetSetting(string type)
    {
        return await context.Settings.FirstOrDefaultAsync(x => x.Type == type);
    }

    public async Task<IEnumerable<Klacks.Api.Domain.Models.Settings.Settings>> GetSettingsList()
    {
        return await context.Settings.ToListAsync();
    }

    public Task<Klacks.Api.Domain.Models.Settings.Settings> PutSetting(Klacks.Api.Domain.Models.Settings.Settings settings)
    {
        context.Settings.Update(settings);
        return Task.FromResult(settings);
    }

    #endregion Setting

    #region Macro

    public Macro AddMacro(Macro macro)
    {
        return _macroManagementService.AddMacroAsync(macro).Result;
    }

    public async Task<Macro> DeleteMacro(Guid id)
    {
        return await _macroManagementService.DeleteMacroAsync(id);
    }

    public async Task<Macro> GetMacro(Guid id)
    {
        return await _macroManagementService.GetMacroAsync(id);
    }

    public async Task<List<Macro>> GetMacroList()
    {
        return await _macroManagementService.GetMacroListAsync();
    }

    public bool MacroExists(Guid id)
    {
        return _macroManagementService.MacroExistsAsync(id).Result;
    }

    public Macro PutMacro(Macro macro)
    {
        return _macroManagementService.UpdateMacroAsync(macro).Result;
    }

    public void RemoveMacro(Macro macro)
    {
        context.Macro.Remove(macro);
    }

    #endregion Macro

    #region MacroType

    public MacroType AddMacroType(MacroType macroType)
    {
        return _macroTypeManagementService.AddMacroTypeAsync(macroType).Result;
    }

    public async Task<MacroType> DeleteMacroType(Guid id)
    {
        return await _macroTypeManagementService.DeleteMacroTypeAsync(id);
    }

    public async Task<MacroType> GetMacroType(Guid id)
    {
        return await _macroTypeManagementService.GetMacroTypeAsync(id);
    }

    public async Task<List<MacroType>> GetOriginalMacroTypeList()
    {
        return await _macroTypeManagementService.GetMacroTypeListAsync();
    }

    public bool MacroTypeExists(Guid id)
    {
        return _macroTypeManagementService.MacroTypeExistsAsync(id).Result;
    }

    public MacroType PutMacroType(MacroType macroType)
    {
        return _macroTypeManagementService.UpdateMacroTypeAsync(macroType).Result;
    }

    public void RemoveMacroType(MacroType macroType)
    {
        context.MacroType.Remove(macroType);
    }

    #endregion MacroType

    #region Vat

    public Vat AddVAT(Vat vat)
    {
        return _vatManagementService.AddVatAsync(vat).Result;
    }

    public async Task<Vat> DeleteVAT(Guid id)
    {
        return await _vatManagementService.DeleteVatAsync(id);
    }

    public async Task<Vat> GetVAT(Guid id)
    {
        return await _vatManagementService.GetVatAsync(id);
    }

    public async Task<List<Vat>> GetVATList()
    {
        return await _vatManagementService.GetVatListAsync();
    }

    public Vat PutVAT(Vat vat)
    {
        return _vatManagementService.UpdateVatAsync(vat).Result;
    }

    public void RemoveVAT(Vat vat)
    {
        context.Vat.Remove(vat);
    }

    public bool VATExists(Guid id)
    {
        return _vatManagementService.VatExistsAsync(id).Result;
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
        var query = context.CalendarRule.AsQueryable();
        
        query = _filterService.ApplyFilters(query, filter);
        query = _sortingService.ApplySorting(query, filter.OrderBy, filter.SortOrder, filter.Language);
        
        return await _paginationService.ApplyPaginationAsync(query, filter);
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

}
