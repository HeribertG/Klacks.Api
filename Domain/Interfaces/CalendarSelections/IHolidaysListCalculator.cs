// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Services.Holidays;

namespace Klacks.Api.Domain.Interfaces.CalendarSelections;

public interface IHolidaysListCalculator
{
    #region Properties
    int CurrentYear { get; set; }
    
    List<CalendarRule> Rules { get; }
   
    List<HolidayDate> HolidayList { get; }
    
    int Count { get; }
    #endregion

    #region Rule Management
    void Add(CalendarRule rule);
    
    void AddRange(IEnumerable<CalendarRule> rules);
    
    void Remove(int index);
    
    CalendarRule? GetRule(int index);
    
    void Clear();
    #endregion

    #region Holiday Computation
    void ComputeHolidays();
   
    DateOnly CalculateEaster(int year);
    #endregion

    #region Utility Methods
    int GetIso8601WeekNumber(DateOnly inputDate);
    
    string FormatDate(DateOnly date);
    
    HolidayStatus IsHoliday(DateOnly currentDate);
    
    HolidayDate? GetHolidayInfo(DateOnly currentDate);
    
    int GetDayOfYear(DateOnly date);
    
    int GetTotalDaysInCurrentYear();
    
    int GetDaysInMonth(int month, int year);
    
    bool IsLeapYear(int year);
    #endregion
}