using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.DTOs.Filter;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Settings;

public class SettingsRepository : ISettingsRepository
{
    private readonly DataBaseContext context;
    private readonly ICalendarRuleFilterService _filterService;
    private readonly ICalendarRuleSortingService _sortingService;
    private readonly ICalendarRulePaginationService _paginationService;
    private readonly IMacroManagementService _macroManagementService;

    public SettingsRepository(DataBaseContext context,
        ICalendarRuleFilterService filterService,
        ICalendarRuleSortingService sortingService,
        ICalendarRulePaginationService paginationService,
        IMacroManagementService macroManagementService)
    {
        this.context = context;
        _filterService = filterService;
        _sortingService = sortingService;
        _paginationService = paginationService;
        _macroManagementService = macroManagementService;
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
