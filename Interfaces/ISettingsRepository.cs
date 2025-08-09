using Klacks.Api.Models.Associations;
using Klacks.Api.Models.Settings;
using Klacks.Api.Presentation.Resources;
using Klacks.Api.Presentation.Resources.Filter;

namespace Klacks.Api.Interfaces;

public interface ISettingsRepository
{

    CalendarRule AddCalendarRule(CalendarRule calendarRule);

    Macro AddMacro(Macro macro);

    MacroType AddMacroType(MacroType macroType);

    Task<Models.Settings.Settings> AddSetting(Models.Settings.Settings settings);

    Vat AddVAT(Vat vat);

    bool CalendarRuleExists(Guid id);

    Task<CalendarRule> DeleteCalendarRule(Guid id);

    Task<Macro> DeleteMacro(Guid id);

    Task<MacroType> DeleteMacroType(Guid id);

    Task<Vat> DeleteVAT(Guid id);

    Task<CalendarRule> GetCalendarRule(Guid id);

    Task<List<CalendarRule>> GetCalendarRuleList();

    Task<Macro> GetMacro(Guid id);

    Task<List<Macro>> GetMacroList();

    Task<MacroType> GetMacroType(Guid id);

    Task<List<MacroType>> GetOriginalMacroTypeList();

    Task<Models.Settings.Settings?> GetSetting(string type);

    Task<IEnumerable<Models.Settings.Settings>> GetSettingsList();

    Task<TruncatedCalendarRule> GetTruncatedCalendarRuleList(CalendarRulesFilter filter);

    Task<Vat> GetVAT(Guid id);

    Task<List<Vat>> GetVATList();

    bool MacroExists(Guid id);

    bool MacroTypeExists(Guid id);

    CalendarRule PutCalendarRule(CalendarRule calendarRule);

    Macro PutMacro(Macro macro);

    MacroType PutMacroType(MacroType macroType);

    Task<Models.Settings.Settings> PutSetting(Models.Settings.Settings settings);

    Vat PutVAT(Vat vat);

    void RemoveCalendarRule(CalendarRule calendarRule);

    void RemoveMacro(Macro macro);

    void RemoveMacroType(MacroType macroType);

    void RemoveVAT(Vat vat);

    bool VATExists(Guid id);
}
