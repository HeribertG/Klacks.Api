// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Macros;

public class MacroData
{
    public decimal Hour { get; set; }
    public string FromHour { get; set; } = string.Empty;
    public string UntilHour { get; set; } = string.Empty;
    public int Weekday { get; set; }
    public bool Holiday { get; set; }
    public bool HolidayNextDay { get; set; }
    public decimal NightRate { get; set; }
    public decimal HolidayRate { get; set; }
    public decimal WE1Rate { get; set; }
    public decimal WE2Rate { get; set; }
    public decimal WE3Rate { get; set; }
    public decimal GuaranteedHours { get; set; }
    public decimal FullTime { get; set; }

    /// <summary>
    /// ISO weekday number (Monday=1..Sunday=7) of the 1st/2nd/3rd configured weekend day, or 0 when that slot
    /// is unused (fewer weekend days configured), so macros can compare a segment's own weekday number against
    /// the operator's configured weekend instead of literal Saturday(6)/Sunday(7). Country-neutral: the three
    /// slots pair with WE1Rate/WE2Rate/WE3Rate respectively.
    /// </summary>
    public int WeekendDay1 { get; set; }
    public int WeekendDay2 { get; set; }
    public int WeekendDay3 { get; set; }
}
