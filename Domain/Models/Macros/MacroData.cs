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
    public decimal SaRate { get; set; }
    public decimal SoRate { get; set; }
    public decimal GuaranteedHours { get; set; }
    public decimal FullTime { get; set; }

    /// <summary>
    /// ISO weekday number (Monday=1..Sunday=7) of the 1st/2nd configured weekend day, or 0 when that slot
    /// is unused (fewer than 1/2 weekend days configured), so macros can compare a segment's own weekday
    /// number against the operator's configured weekend instead of literal Saturday(6)/Sunday(7).
    /// </summary>
    public int WeekendDay1 { get; set; }
    public int WeekendDay2 { get; set; }
}
