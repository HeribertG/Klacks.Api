using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Application.DTOs;
using Klacks.Api.Application.DTOs.Filter;

namespace Klacks.Api.Application.Interfaces;

public interface ISettingsRepository
{

    CalendarRule AddCalendarRule(CalendarRule calendarRule);

    Macro AddMacro(Macro macro);

    Task<Klacks.Api.Domain.Models.Settings.Settings> AddSetting(Klacks.Api.Domain.Models.Settings.Settings settings);

    bool CalendarRuleExists(Guid id);

    Task<CalendarRule> DeleteCalendarRule(Guid id);

    Task<Macro> DeleteMacro(Guid id);

    Task<CalendarRule> GetCalendarRule(Guid id);

    Task<List<CalendarRule>> GetCalendarRuleList();

    Task<Macro> GetMacro(Guid id);

    Task<List<Macro>> GetMacroList();

    Task<Klacks.Api.Domain.Models.Settings.Settings?> GetSetting(string type);

    Task<IEnumerable<Klacks.Api.Domain.Models.Settings.Settings>> GetSettingsList();

    Task<TruncatedCalendarRule> GetTruncatedCalendarRuleList(CalendarRulesFilter filter);

    bool MacroExists(Guid id);

    CalendarRule PutCalendarRule(CalendarRule calendarRule);

    Macro PutMacro(Macro macro);

    Task<Klacks.Api.Domain.Models.Settings.Settings> PutSetting(Klacks.Api.Domain.Models.Settings.Settings settings);

    void RemoveCalendarRule(CalendarRule calendarRule);

    void RemoveMacro(Macro macro);
}
