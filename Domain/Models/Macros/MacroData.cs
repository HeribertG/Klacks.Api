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
}
