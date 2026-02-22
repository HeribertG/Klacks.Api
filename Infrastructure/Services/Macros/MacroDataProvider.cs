using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Holidays;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Macros;

public class MacroDataProvider : IMacroDataProvider
{
    private readonly DataBaseContext _context;
    private readonly IHolidayCalculatorCache _holidayCache;

    public MacroDataProvider(DataBaseContext context, IHolidayCalculatorCache holidayCache)
    {
        _context = context;
        _holidayCache = holidayCache;
    }

    public async Task<MacroData> GetMacroDataAsync(Work work)
    {
        var workDate = work.CurrentDate;
        var workDateNextDay = workDate.AddDays(1);

        var contract = await GetActiveContractAsync(work.ClientId, workDate);
        var defaultSettings = await GetDefaultSettingsAsync();

        var macroData = new MacroData
        {
            Hour = work.WorkTime,
            FromHour = work.StartTime.ToString("HH:mm"),
            UntilHour = work.EndTime.ToString("HH:mm"),
            Weekday = ConvertToIsoWeekday(work.CurrentDate.DayOfWeek),
            NightRate = contract?.NightRate ?? defaultSettings.NightRate,
            HolidayRate = contract?.HolidayRate ?? defaultSettings.HolidayRate,
            SaRate = contract?.SaRate ?? defaultSettings.SaRate,
            SoRate = contract?.SoRate ?? defaultSettings.SoRate,
            GuaranteedHours = contract?.GuaranteedHours ?? defaultSettings.GuaranteedHours,
            FullTime = contract?.FullTime ?? defaultSettings.FullTime
        };

        IHolidaysListCalculator? calculator = null;
        IHolidaysListCalculator? calculatorNextDay = null;

        if (contract?.CalendarSelectionId != null)
        {
            calculator = await GetHolidayCalculatorAsync(contract.CalendarSelectionId.Value, workDate.Year);
            calculatorNextDay = workDateNextDay.Year != workDate.Year
                ? await GetHolidayCalculatorAsync(contract.CalendarSelectionId.Value, workDateNextDay.Year)
                : calculator;
        }
        else
        {
            var globalSettings = await GetGlobalCalendarSettingsAsync();
            if (globalSettings.CalendarSelectionId != null)
            {
                calculator = await GetHolidayCalculatorAsync(globalSettings.CalendarSelectionId.Value, workDate.Year);
                calculatorNextDay = workDateNextDay.Year != workDate.Year
                    ? await GetHolidayCalculatorAsync(globalSettings.CalendarSelectionId.Value, workDateNextDay.Year)
                    : calculator;
            }
            else if (!string.IsNullOrEmpty(globalSettings.Country) && !string.IsNullOrEmpty(globalSettings.State))
            {
                calculator = await GetHolidayCalculatorByCountryStateAsync(globalSettings.Country, globalSettings.State, workDate.Year);
                calculatorNextDay = workDateNextDay.Year != workDate.Year
                    ? await GetHolidayCalculatorByCountryStateAsync(globalSettings.Country, globalSettings.State, workDateNextDay.Year)
                    : calculator;
            }
        }

        if (calculator != null)
        {
            macroData.Holiday = calculator.IsHoliday(workDate) == HolidayStatus.OfficialHoliday;
            macroData.HolidayNextDay = calculatorNextDay?.IsHoliday(workDateNextDay) == HolidayStatus.OfficialHoliday;
        }

        return macroData;
    }

    private async Task<(string? Country, string? State, Guid? CalendarSelectionId)> GetGlobalCalendarSettingsAsync()
    {
        var settingTypes = new[] { SettingKeys.GlobalCalendarCountry, SettingKeys.GlobalCalendarState, SettingKeys.GlobalCalendarSelectionId };
        var settings = await _context.Settings
            .Where(s => settingTypes.Contains(s.Type))
            .ToListAsync();

        var country = settings.FirstOrDefault(s => s.Type == SettingKeys.GlobalCalendarCountry)?.Value;
        var state = settings.FirstOrDefault(s => s.Type == SettingKeys.GlobalCalendarState)?.Value;
        var calendarSelectionIdStr = settings.FirstOrDefault(s => s.Type == SettingKeys.GlobalCalendarSelectionId)?.Value;

        Guid? calendarSelectionId = null;
        if (!string.IsNullOrEmpty(calendarSelectionIdStr) && Guid.TryParse(calendarSelectionIdStr, out var parsedGuid))
        {
            calendarSelectionId = parsedGuid;
        }

        return (country, state, calendarSelectionId);
    }

    private async Task<IHolidaysListCalculator> GetHolidayCalculatorByCountryStateAsync(string country, string state, int year)
    {
        var calculator = new HolidaysListCalculator { CurrentYear = year };
        var rules = await LoadCalendarRulesByCountryState(country, state);
        calculator.AddRange(rules);
        calculator.ComputeHolidays();
        return calculator;
    }

    private async Task<List<Domain.Models.Settings.CalendarRule>> LoadCalendarRulesByCountryState(string country, string state)
    {
        return await _context.CalendarRule
            .Where(cr => cr.Country == country && cr.State == state)
            .ToListAsync();
    }

    private async Task<DefaultMacroSettings> GetDefaultSettingsAsync()
    {
        var settingTypes = new[] { SettingKeys.NightRate, SettingKeys.HolidayRate, SettingKeys.SaRate, SettingKeys.SoRate, SettingKeys.GuaranteedHours, SettingKeys.FullTime };

        var settings = await _context.Settings
            .Where(s => settingTypes.Contains(s.Type))
            .ToDictionaryAsync(s => s.Type, s => s.Value);

        return new DefaultMacroSettings
        {
            NightRate = ParseDecimal(settings.GetValueOrDefault(SettingKeys.NightRate)),
            HolidayRate = ParseDecimal(settings.GetValueOrDefault(SettingKeys.HolidayRate)),
            SaRate = ParseDecimal(settings.GetValueOrDefault(SettingKeys.SaRate)),
            SoRate = ParseDecimal(settings.GetValueOrDefault(SettingKeys.SoRate)),
            GuaranteedHours = ParseDecimal(settings.GetValueOrDefault(SettingKeys.GuaranteedHours)),
            FullTime = ParseDecimal(settings.GetValueOrDefault(SettingKeys.FullTime))
        };
    }

    private static decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    private record DefaultMacroSettings
    {
        public decimal NightRate { get; init; }
        public decimal HolidayRate { get; init; }
        public decimal SaRate { get; init; }
        public decimal SoRate { get; init; }
        public decimal GuaranteedHours { get; init; }
        public decimal FullTime { get; init; }
    }

    private async Task<Domain.Models.Associations.Contract?> GetActiveContractAsync(Guid clientId, DateOnly date)
    {
        var clientContract = await _context.Set<Domain.Models.Staffs.ClientContract>()
            .Include(cc => cc.Contract)
            .Where(cc => cc.ClientId == clientId)
            .Where(cc => cc.FromDate <= date)
            .Where(cc => cc.UntilDate == null || cc.UntilDate >= date)
            .Where(cc => cc.IsActive)
            .OrderByDescending(cc => cc.FromDate)
            .FirstOrDefaultAsync();

        return clientContract?.Contract;
    }

    private async Task<IHolidaysListCalculator> GetHolidayCalculatorAsync(Guid calendarSelectionId, int year)
    {
        return _holidayCache.GetOrCreate(calendarSelectionId, year, () =>
        {
            var calculator = new HolidaysListCalculator { CurrentYear = year };
            var rules = LoadCalendarRulesForSelection(calendarSelectionId).Result;
            calculator.AddRange(rules);
            calculator.ComputeHolidays();
            return calculator;
        });
    }

    private async Task<List<Domain.Models.Settings.CalendarRule>> LoadCalendarRulesForSelection(Guid calendarSelectionId)
    {
        var selectedCalendars = await _context.Set<Domain.Models.CalendarSelections.SelectedCalendar>()
            .Where(sc => sc.CalendarSelectionId == calendarSelectionId)
            .Select(sc => new { sc.Country, sc.State })
            .ToListAsync();

        if (selectedCalendars.Count == 0)
        {
            return new List<Domain.Models.Settings.CalendarRule>();
        }

        var countries = selectedCalendars.Select(sc => sc.Country).Distinct().ToList();
        var states = selectedCalendars.Select(sc => sc.State).Distinct().ToList();

        var rules = await _context.CalendarRule
            .Where(cr => countries.Contains(cr.Country) && states.Contains(cr.State))
            .ToListAsync();

        return rules
            .Where(cr => selectedCalendars.Any(sc => sc.Country == cr.Country && sc.State == cr.State))
            .ToList();
    }

    private static int ConvertToIsoWeekday(DayOfWeek dayOfWeek)
    {
        // C# DayOfWeek: Sunday=0, Monday=1, ..., Saturday=6
        // ISO-8601/Frontend: Monday=1, Tuesday=2, ..., Sunday=7
        return dayOfWeek == DayOfWeek.Sunday ? 7 : (int)dayOfWeek;
    }

    public async Task<MacroData> GetMacroDataForWorkChangeAsync(WorkChange workChange, Work work)
    {
        var shift = await _context.Shift.FirstOrDefaultAsync(s => s.Id == work.ShiftId);

        var workChangeDate = GetWorkChangeDateWithMidnightRule(workChange, work, shift);
        var workChangeDateNextDay = workChangeDate.AddDays(1);

        var contract = await GetActiveContractAsync(work.ClientId, workChangeDate);
        var defaultSettings = await GetDefaultSettingsAsync();

        var macroData = new MacroData
        {
            Hour = workChange.ChangeTime,
            FromHour = workChange.StartTime.ToString("HH:mm"),
            UntilHour = workChange.EndTime.ToString("HH:mm"),
            Weekday = ConvertToIsoWeekday(workChangeDate.DayOfWeek),
            NightRate = contract?.NightRate ?? defaultSettings.NightRate,
            HolidayRate = contract?.HolidayRate ?? defaultSettings.HolidayRate,
            SaRate = contract?.SaRate ?? defaultSettings.SaRate,
            SoRate = contract?.SoRate ?? defaultSettings.SoRate,
            GuaranteedHours = contract?.GuaranteedHours ?? defaultSettings.GuaranteedHours,
            FullTime = contract?.FullTime ?? defaultSettings.FullTime
        };

        IHolidaysListCalculator? calculator = null;
        IHolidaysListCalculator? calculatorNextDay = null;

        if (contract?.CalendarSelectionId != null)
        {
            calculator = await GetHolidayCalculatorAsync(contract.CalendarSelectionId.Value, workChangeDate.Year);
            calculatorNextDay = workChangeDateNextDay.Year != workChangeDate.Year
                ? await GetHolidayCalculatorAsync(contract.CalendarSelectionId.Value, workChangeDateNextDay.Year)
                : calculator;
        }
        else
        {
            var globalSettings = await GetGlobalCalendarSettingsAsync();
            if (globalSettings.CalendarSelectionId != null)
            {
                calculator = await GetHolidayCalculatorAsync(globalSettings.CalendarSelectionId.Value, workChangeDate.Year);
                calculatorNextDay = workChangeDateNextDay.Year != workChangeDate.Year
                    ? await GetHolidayCalculatorAsync(globalSettings.CalendarSelectionId.Value, workChangeDateNextDay.Year)
                    : calculator;
            }
            else if (!string.IsNullOrEmpty(globalSettings.Country) && !string.IsNullOrEmpty(globalSettings.State))
            {
                calculator = await GetHolidayCalculatorByCountryStateAsync(globalSettings.Country, globalSettings.State, workChangeDate.Year);
                calculatorNextDay = workChangeDateNextDay.Year != workChangeDate.Year
                    ? await GetHolidayCalculatorByCountryStateAsync(globalSettings.Country, globalSettings.State, workChangeDateNextDay.Year)
                    : calculator;
            }
        }

        if (calculator != null)
        {
            macroData.Holiday = calculator.IsHoliday(workChangeDate) == HolidayStatus.OfficialHoliday;
            macroData.HolidayNextDay = calculatorNextDay?.IsHoliday(workChangeDateNextDay) == HolidayStatus.OfficialHoliday;
        }

        return macroData;
    }

    private static DateOnly GetWorkChangeDateWithMidnightRule(WorkChange workChange, Work work, Shift? shift)
    {
        if (shift == null)
        {
            return work.CurrentDate;
        }

        var isMidnightShift = shift.EndShift < shift.StartShift;
        if (!isMidnightShift)
        {
            return work.CurrentDate;
        }

        var isAfterMidnight = workChange.StartTime < shift.StartShift;
        return isAfterMidnight ? work.CurrentDate.AddDays(1) : work.CurrentDate;
    }
}
