// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Scheduling;

public class SchedulingRule : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public int? MaxWorkDays { get; set; }

    public int? MinRestDays { get; set; }

    public decimal? MinPauseHours { get; set; }

    public decimal? MaxOptimalGap { get; set; }

    public decimal? MaxDailyHours { get; set; }

    public decimal? MaxWeeklyHours { get; set; }

    public int? MaxConsecutiveDays { get; set; }

    public decimal? DefaultWorkingHours { get; set; }

    public decimal? OvertimeThreshold { get; set; }

    public decimal? GuaranteedHours { get; set; }

    public decimal? MaximumHours { get; set; }

    public decimal? MinimumHours { get; set; }

    public decimal? FullTimeHours { get; set; }

    public int? VacationDaysPerYear { get; set; }

    public decimal? NightRate { get; set; }

    public decimal? HolidayRate { get; set; }

    public decimal? SaRate { get; set; }

    public decimal? SoRate { get; set; }

    public bool? WorkOnMonday { get; set; }

    public bool? WorkOnTuesday { get; set; }

    public bool? WorkOnWednesday { get; set; }

    public bool? WorkOnThursday { get; set; }

    public bool? WorkOnFriday { get; set; }

    public bool? WorkOnSaturday { get; set; }

    public bool? WorkOnSunday { get; set; }

    public bool? PerformsShiftWork { get; set; }
}
