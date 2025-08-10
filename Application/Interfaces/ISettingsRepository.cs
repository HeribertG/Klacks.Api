using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Presentation.DTOs;
using Klacks.Api.Presentation.DTOs.Filter;

namespace Klacks.Api.Application.Interfaces;

public interface ISettingsRepository
{

    CalendarRule AddCalendarRule(CalendarRule calendarRule);

    Macro AddMacro(Macro macro);

    MacroType AddMacroType(MacroType macroType);

    Task<Klacks.Api.Domain.Models.Settings.Settings> AddSetting(Klacks.Api.Domain.Models.Settings.Settings settings);

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

    Task<Klacks.Api.Domain.Models.Settings.Settings?> GetSetting(string type);

    Task<IEnumerable<Klacks.Api.Domain.Models.Settings.Settings>> GetSettingsList();

    Task<TruncatedCalendarRule> GetTruncatedCalendarRuleList(CalendarRulesFilter filter);

    Task<Vat> GetVAT(Guid id);

    Task<List<Vat>> GetVATList();

    bool MacroExists(Guid id);

    bool MacroTypeExists(Guid id);

    CalendarRule PutCalendarRule(CalendarRule calendarRule);

    Macro PutMacro(Macro macro);

    MacroType PutMacroType(MacroType macroType);

    Task<Klacks.Api.Domain.Models.Settings.Settings> PutSetting(Klacks.Api.Domain.Models.Settings.Settings settings);

    Vat PutVAT(Vat vat);

    void RemoveCalendarRule(CalendarRule calendarRule);

    void RemoveMacro(Macro macro);

    void RemoveMacroType(MacroType macroType);

    void RemoveVAT(Vat vat);

    bool VATExists(Guid id);
}
