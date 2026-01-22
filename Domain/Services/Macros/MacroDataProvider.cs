using System.Globalization;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Holidays;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.Macros;

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
        var workDate = DateOnly.FromDateTime(work.CurrentDate);
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

        if (contract?.CalendarSelectionId != null)
        {
            var calculator = await GetHolidayCalculatorAsync(contract.CalendarSelectionId.Value, workDate.Year);
            macroData.Holiday = calculator.IsHoliday(workDate) == HolidayStatus.OfficialHoliday;

            var calculatorNextDay = workDateNextDay.Year != workDate.Year
                ? await GetHolidayCalculatorAsync(contract.CalendarSelectionId.Value, workDateNextDay.Year)
                : calculator;
            macroData.HolidayNextDay = calculatorNextDay.IsHoliday(workDateNextDay) == HolidayStatus.OfficialHoliday;
        }

        return macroData;
    }

    private async Task<DefaultMacroSettings> GetDefaultSettingsAsync()
    {
        var settingTypes = new[] { "nightRate", "holidayRate", "saRate", "soRate", "guaranteedHours", "fullTime" };

        var settings = await _context.Settings
            .Where(s => settingTypes.Contains(s.Type))
            .ToDictionaryAsync(s => s.Type, s => s.Value);

        return new DefaultMacroSettings
        {
            NightRate = ParseDecimal(settings.GetValueOrDefault("nightRate")),
            HolidayRate = ParseDecimal(settings.GetValueOrDefault("holidayRate")),
            SaRate = ParseDecimal(settings.GetValueOrDefault("saRate")),
            SoRate = ParseDecimal(settings.GetValueOrDefault("soRate")),
            GuaranteedHours = ParseDecimal(settings.GetValueOrDefault("guaranteedHours")),
            FullTime = ParseDecimal(settings.GetValueOrDefault("fullTime"))
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

        var rules = await _context.CalendarRule
            .Where(cr => selectedCalendars.Any(sc => sc.Country == cr.Country && sc.State == cr.State))
            .ToListAsync();

        return rules;
    }

    private static int ConvertToIsoWeekday(DayOfWeek dayOfWeek)
    {
        // C# DayOfWeek: Sunday=0, Monday=1, ..., Saturday=6
        // ISO-8601/Frontend: Monday=1, Tuesday=2, ..., Sunday=7
        return dayOfWeek == DayOfWeek.Sunday ? 7 : (int)dayOfWeek;
    }
}
