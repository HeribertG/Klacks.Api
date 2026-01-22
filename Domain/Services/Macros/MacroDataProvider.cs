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

        var macroData = new MacroData
        {
            Hour = work.WorkTime,
            FromHour = work.StartTime.ToString("HH:mm"),
            UntilHour = work.EndTime.ToString("HH:mm"),
            Weekday = ConvertToIsoWeekday(work.CurrentDate.DayOfWeek),
            NightRate = contract?.NightRate ?? 0,
            HolidayRate = contract?.HolidayRate ?? 0,
            SaRate = contract?.SaRate ?? 0,
            SoRate = contract?.SoRate ?? 0,
            GuaranteedHours = contract?.GuaranteedHours ?? 0,
            FullTime = contract?.FullTime ?? 0
        };

        if (contract?.CalendarSelectionId != null)
        {
            var calculator = await GetHolidayCalculatorAsync(contract.CalendarSelectionId.Value, workDate.Year);
            macroData.Holiday = calculator.IsHoliday(workDate) == HolidayStatus.OfficialHoliday;
            macroData.HolidayNextDay = calculator.IsHoliday(workDateNextDay) == HolidayStatus.OfficialHoliday;
        }

        return macroData;
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
