using Klacks.Api.Models.Associations;
using Klacks.Api.Models.Settings;
using Klacks.Api.Resources;
using Klacks.Api.Resources.Filter;

namespace Klacks.Api.Interfaces;

public interface ISettingsRepository
{
  BankDetails AddBankDetail(BankDetails bankDetails);

  CalendarRule AddCalendarRule(CalendarRule calendarRule);

  Macro AddMacro(Macro macro);

  MacroType AddMacroType(MacroType macroType);

  Task<Models.Settings.Settings> AddSetting(Models.Settings.Settings settings);

  Vat AddVAT(Vat vat);

  bool BankDetailExists(Guid id);

  bool CalendarRuleExists(Guid id);

  HttpResultResource CreateExcelFile(CalendarRulesFilter filter);

  Task<BankDetails> DeleteBankDetail(Guid id);

  Task<CalendarRule> DeleteCalendarRule(Guid id);

  Task<Macro> DeleteMacro(Guid id);

  Task<MacroType> DeleteMacroType(Guid id);

  Task<Vat> DeleteVAT(Guid id);

  Task<BankDetails> GetBankDetail(Guid id);

  Task<List<BankDetails>> GetBankDetailList();

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

  BankDetails PutBankDetail(BankDetails bankDetails);

  CalendarRule PutCalendarRule(CalendarRule calendarRule);

  Macro PutMacro(Macro macro);

  MacroType PutMacroType(MacroType macroType);

  Task<Models.Settings.Settings> PutSetting(Models.Settings.Settings settings);

  Vat PutVAT(Vat vat);

  void RemoveBankDetail(BankDetails bankDetails);

  void RemoveCalendarRule(CalendarRule calendarRule);

  void RemoveMacro(Macro macro);

  void RemoveMacroType(MacroType macroType);

  void RemoveVAT(Vat vat);

  bool VATExists(Guid id);
}
