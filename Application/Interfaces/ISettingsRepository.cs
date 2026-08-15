// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Application.DTOs;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Application.DTOs.Filter;

namespace Klacks.Api.Application.Interfaces;

public interface ISettingsRepository : ISettingsReader
{

    CalendarRule AddCalendarRule(CalendarRule calendarRule);

    Task<Macro> AddMacroAsync(Macro macro);

    Task<Klacks.Api.Domain.Models.Settings.Settings> AddSetting(Klacks.Api.Domain.Models.Settings.Settings settings);

    bool CalendarRuleExists(Guid id);

    Task<CalendarRule> DeleteCalendarRule(Guid id);

    Task<Macro> DeleteMacro(Guid id);

    Task<CalendarRule> GetCalendarRule(Guid id);

    Task<List<CalendarRule>> GetCalendarRuleList();

    Task<Macro?> GetMacro(Guid id);

    Task<List<Macro>> GetMacroList();

    Task<Klacks.Api.Domain.Models.Settings.Settings?> GetSettingNoTracking(string type);

    Task<IEnumerable<Klacks.Api.Domain.Models.Settings.Settings>> GetSettingsList();

    Task<TruncatedCalendarRule> GetTruncatedCalendarRuleList(CalendarRulesFilter filter);

    Task<bool> MacroExistsAsync(Guid id);

    CalendarRule PutCalendarRule(CalendarRule calendarRule);

    Task<Macro> PutMacroAsync(Macro macro);

    Task<Klacks.Api.Domain.Models.Settings.Settings> PutSetting(Klacks.Api.Domain.Models.Settings.Settings settings);

    void RemoveCalendarRule(CalendarRule calendarRule);

    void RemoveMacro(Macro macro);

    Task UpsertSettingAsync(string type, string value);

    /// <summary>
    /// Writes a new value for a setting only if it still holds the value the caller last read, and
    /// reports whether this caller won. The check and the write are one statement, so two callers --
    /// a second API instance, or an overlapping run -- cannot both advance the same value.
    /// </summary>
    /// <param name="type">Key of the setting to advance</param>
    /// <param name="expectedValue">Value the caller read; a row that changed since then yields false</param>
    /// <param name="newValue">Value written when the expectation still holds</param>
    Task<bool> TryAdvanceSettingAsync(string type, string expectedValue, string newValue);
}
