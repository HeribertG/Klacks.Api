// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.CalendarSelections;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Holidays;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Macros;

public class MacroDataProvider : IMacroDataProvider
{
    private readonly DataBaseContext _context;
    private readonly IHolidayCalculatorCache _holidayCache;
    private readonly IClientContractDataProvider _contractDataProvider;
    private readonly IWorkChangeEffectiveTimeService _effectiveTimeService;
    private readonly IWeekConfiguration _weekConfiguration;

    public MacroDataProvider(
        DataBaseContext context,
        IHolidayCalculatorCache holidayCache,
        IClientContractDataProvider contractDataProvider,
        IWorkChangeEffectiveTimeService effectiveTimeService,
        IWeekConfiguration weekConfiguration)
    {
        _context = context;
        _holidayCache = holidayCache;
        _contractDataProvider = contractDataProvider;
        _effectiveTimeService = effectiveTimeService;
        _weekConfiguration = weekConfiguration;
    }

    public async Task<MacroData> GetMacroDataAsync(Work work)
    {
        var workDate = work.CurrentDate;
        var workDateNextDay = workDate.AddDays(1);

        var effectiveData = await _contractDataProvider.GetEffectiveContractDataAsync(work.ClientId, workDate);

        var macroData = new MacroData
        {
            Hour = work.WorkTime,
            FromHour = work.StartTime.ToString("HH:mm"),
            UntilHour = work.EndTime.ToString("HH:mm"),
            Weekday = ConvertToIsoWeekday(work.CurrentDate.DayOfWeek),
            NightRate = effectiveData.NightRate,
            HolidayRate = effectiveData.HolidayRate,
            WE1Rate = effectiveData.WE1Rate,
            WE2Rate = effectiveData.WE2Rate,
            WE3Rate = effectiveData.WE3Rate,
            GuaranteedHours = effectiveData.GuaranteedHours,
            FullTime = effectiveData.FullTime
        };

        await ApplyHolidayData(macroData, effectiveData.CalendarSelectionId, workDate, workDateNextDay);
        await ApplyWeekendFlags(macroData);

        return macroData;
    }

    public async Task<MacroData> GetMacroDataForBreakAsync(Break breakEntry, int? paymentInterval = null)
    {
        var breakDate = breakEntry.CurrentDate;
        var breakDateNextDay = breakDate.AddDays(1);

        var effectiveData = await _contractDataProvider.GetEffectiveContractDataAsync(breakEntry.ClientId, breakDate, paymentInterval);

        var macroData = new MacroData
        {
            Hour = effectiveData.DefaultWorkingHours,
            FromHour = breakEntry.StartTime.ToString("HH:mm"),
            UntilHour = breakEntry.EndTime.ToString("HH:mm"),
            Weekday = ConvertToIsoWeekday(breakEntry.CurrentDate.DayOfWeek),
            NightRate = effectiveData.NightRate,
            HolidayRate = effectiveData.HolidayRate,
            WE1Rate = effectiveData.WE1Rate,
            WE2Rate = effectiveData.WE2Rate,
            WE3Rate = effectiveData.WE3Rate,
            GuaranteedHours = effectiveData.GuaranteedHours,
            FullTime = effectiveData.FullTime
        };

        await ApplyHolidayData(macroData, effectiveData.CalendarSelectionId, breakDate, breakDateNextDay);
        await ApplyWeekendFlags(macroData);

        return macroData;
    }

    public async Task<MacroData> GetMacroDataForWorkChangeAsync(WorkChange workChange, Work work)
    {
        var shift = await _context.Shift.FirstOrDefaultAsync(s => s.Id == work.ShiftId);
        var (effectiveStart, effectiveEnd) = await _effectiveTimeService.GetEffectiveTimesAsync(workChange, work, shift);

        var workChangeDate = GetWorkChangeDateWithMidnightRule(effectiveStart, work, shift);
        var workChangeDateNextDay = workChangeDate.AddDays(1);

        var effectiveData = await _contractDataProvider.GetEffectiveContractDataAsync(work.ClientId, workChangeDate);

        var macroData = new MacroData
        {
            Hour = workChange.ChangeTime,
            FromHour = effectiveStart.ToString("HH:mm"),
            UntilHour = effectiveEnd.ToString("HH:mm"),
            Weekday = ConvertToIsoWeekday(workChangeDate.DayOfWeek),
            NightRate = effectiveData.NightRate,
            HolidayRate = effectiveData.HolidayRate,
            WE1Rate = effectiveData.WE1Rate,
            WE2Rate = effectiveData.WE2Rate,
            WE3Rate = effectiveData.WE3Rate,
            GuaranteedHours = effectiveData.GuaranteedHours,
            FullTime = effectiveData.FullTime
        };

        await ApplyHolidayData(macroData, effectiveData.CalendarSelectionId, workChangeDate, workChangeDateNextDay);
        await ApplyWeekendFlags(macroData);

        return macroData;
    }

    /// <summary>
    /// Sets WeekendDay1/WeekendDay2/WeekendDay3 to the ISO weekday numbers (Monday=1..Sunday=7) of the
    /// 1st/2nd/3rd configured weekend day (ordered by ISO weekday, 0 when unused), so macros can compare a
    /// segment's own weekday number (which may be "tomorrow" across a midnight-crossing shift) against the
    /// operator's configured weekend instead of literal Saturday(6)/Sunday(7).
    /// </summary>
    private async Task ApplyWeekendFlags(MacroData macroData)
    {
        var weekendDays = await _weekConfiguration.GetWeekendDaysAsync();
        var orderedWeekendDays = weekendDays.OrderBy(ConvertToIsoWeekday).ToList();

        macroData.WeekendDay1 = orderedWeekendDays.Count > 0 ? ConvertToIsoWeekday(orderedWeekendDays[0]) : 0;
        macroData.WeekendDay2 = orderedWeekendDays.Count > 1 ? ConvertToIsoWeekday(orderedWeekendDays[1]) : 0;
        macroData.WeekendDay3 = orderedWeekendDays.Count > 2 ? ConvertToIsoWeekday(orderedWeekendDays[2]) : 0;
    }

    private async Task ApplyHolidayData(MacroData macroData, Guid? calendarSelectionId, DateOnly date, DateOnly nextDay)
    {
        IHolidaysListCalculator? calculator = null;
        IHolidaysListCalculator? calculatorNextDay = null;

        if (calendarSelectionId != null)
        {
            calculator = await GetHolidayCalculatorAsync(calendarSelectionId.Value, date.Year);
            calculatorNextDay = nextDay.Year != date.Year
                ? await GetHolidayCalculatorAsync(calendarSelectionId.Value, nextDay.Year)
                : calculator;
        }
        else
        {
            var globalSettings = await GetGlobalCalendarSettingsAsync();
            if (globalSettings.CalendarSelectionId != null)
            {
                calculator = await GetHolidayCalculatorAsync(globalSettings.CalendarSelectionId.Value, date.Year);
                calculatorNextDay = nextDay.Year != date.Year
                    ? await GetHolidayCalculatorAsync(globalSettings.CalendarSelectionId.Value, nextDay.Year)
                    : calculator;
            }
            else if (!string.IsNullOrEmpty(globalSettings.Country) && !string.IsNullOrEmpty(globalSettings.State))
            {
                calculator = await GetHolidayCalculatorByCountryStateAsync(globalSettings.Country, globalSettings.State, date.Year);
                calculatorNextDay = nextDay.Year != date.Year
                    ? await GetHolidayCalculatorByCountryStateAsync(globalSettings.Country, globalSettings.State, nextDay.Year)
                    : calculator;
            }
        }

        if (calculator != null)
        {
            macroData.Holiday = calculator.IsHoliday(date) == HolidayStatus.OfficialHoliday;
            macroData.HolidayNextDay = calculatorNextDay?.IsHoliday(nextDay) == HolidayStatus.OfficialHoliday;
        }
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

    private async Task<IHolidaysListCalculator> GetHolidayCalculatorAsync(Guid calendarSelectionId, int year)
    {
        return await _holidayCache.GetOrCreateAsync(calendarSelectionId, year, async () =>
        {
            var calculator = new HolidaysListCalculator { CurrentYear = year };
            var rules = await LoadCalendarRulesForSelection(calendarSelectionId);
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
        return dayOfWeek == DayOfWeek.Sunday ? 7 : (int)dayOfWeek;
    }

    private static DateOnly GetWorkChangeDateWithMidnightRule(TimeOnly effectiveStart, Work work, Shift? shift)
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

        var isAfterMidnight = effectiveStart < shift.StartShift;
        return isAfterMidnight ? work.CurrentDate.AddDays(1) : work.CurrentDate;
    }
}
