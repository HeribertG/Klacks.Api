// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces.Schedules;
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
    private readonly IClientHolidayCalendarResolver _holidayCalendarResolver;
    private readonly IClientContractDataProvider _contractDataProvider;
    private readonly IWorkChangeEffectiveTimeService _effectiveTimeService;
    private readonly IWeekConfiguration _weekConfiguration;

    public MacroDataProvider(
        DataBaseContext context,
        IClientHolidayCalendarResolver holidayCalendarResolver,
        IClientContractDataProvider contractDataProvider,
        IWorkChangeEffectiveTimeService effectiveTimeService,
        IWeekConfiguration weekConfiguration)
    {
        _context = context;
        _holidayCalendarResolver = holidayCalendarResolver;
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
            NightRateMode = effectiveData.NightRateMode,
            HolidayRateMode = effectiveData.HolidayRateMode,
            WE1RateMode = effectiveData.WE1RateMode,
            WE2RateMode = effectiveData.WE2RateMode,
            WE3RateMode = effectiveData.WE3RateMode,
            NightMinimumPerHour = effectiveData.NightMinimumPerHour,
            HolidayMinimumPerHour = effectiveData.HolidayMinimumPerHour,
            WE1MinimumPerHour = effectiveData.WE1MinimumPerHour,
            WE2MinimumPerHour = effectiveData.WE2MinimumPerHour,
            WE3MinimumPerHour = effectiveData.WE3MinimumPerHour,
            NightStart = effectiveData.NightStart,
            NightEnd = effectiveData.NightEnd,
            GuaranteedHours = effectiveData.GuaranteedHours,
            FullTime = effectiveData.FullTime,
            WorkloadPercent = effectiveData.WorkloadPercent
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
            NightRateMode = effectiveData.NightRateMode,
            HolidayRateMode = effectiveData.HolidayRateMode,
            WE1RateMode = effectiveData.WE1RateMode,
            WE2RateMode = effectiveData.WE2RateMode,
            WE3RateMode = effectiveData.WE3RateMode,
            NightMinimumPerHour = effectiveData.NightMinimumPerHour,
            HolidayMinimumPerHour = effectiveData.HolidayMinimumPerHour,
            WE1MinimumPerHour = effectiveData.WE1MinimumPerHour,
            WE2MinimumPerHour = effectiveData.WE2MinimumPerHour,
            WE3MinimumPerHour = effectiveData.WE3MinimumPerHour,
            NightStart = effectiveData.NightStart,
            NightEnd = effectiveData.NightEnd,
            GuaranteedHours = effectiveData.GuaranteedHours,
            FullTime = effectiveData.FullTime,
            WorkloadPercent = effectiveData.WorkloadPercent
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
            NightRateMode = effectiveData.NightRateMode,
            HolidayRateMode = effectiveData.HolidayRateMode,
            WE1RateMode = effectiveData.WE1RateMode,
            WE2RateMode = effectiveData.WE2RateMode,
            WE3RateMode = effectiveData.WE3RateMode,
            NightMinimumPerHour = effectiveData.NightMinimumPerHour,
            HolidayMinimumPerHour = effectiveData.HolidayMinimumPerHour,
            WE1MinimumPerHour = effectiveData.WE1MinimumPerHour,
            WE2MinimumPerHour = effectiveData.WE2MinimumPerHour,
            WE3MinimumPerHour = effectiveData.WE3MinimumPerHour,
            NightStart = effectiveData.NightStart,
            NightEnd = effectiveData.NightEnd,
            GuaranteedHours = effectiveData.GuaranteedHours,
            FullTime = effectiveData.FullTime,
            WorkloadPercent = effectiveData.WorkloadPercent
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

        calculator = await _holidayCalendarResolver.GetCalculatorAsync(calendarSelectionId, date.Year);
        calculatorNextDay = nextDay.Year != date.Year
            ? await _holidayCalendarResolver.GetCalculatorAsync(calendarSelectionId, nextDay.Year)
            : calculator;

        if (calculator != null)
        {
            macroData.Holiday = calculator.IsHoliday(date) == HolidayStatus.OfficialHoliday;
            macroData.HolidayNextDay = calculatorNextDay?.IsHoliday(nextDay) == HolidayStatus.OfficialHoliday;
        }
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
