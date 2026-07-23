// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.IndustryTemplates;

public class IndustryTemplateSchedulingRuleItem
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal? DefaultWorkingHours { get; set; }

    public decimal? FullTimeHours { get; set; }

    public decimal? MaxDailyHours { get; set; }

    public decimal? MaxWeeklyHours { get; set; }

    public int? MaxConsecutiveDays { get; set; }

    public decimal? MinRestDays { get; set; }

    public decimal? MinPauseHours { get; set; }

    public decimal? OvertimeThreshold { get; set; }

    public string? OvertimeBasis { get; set; }

    public string? OvertimeRateMode { get; set; }

    public decimal? OvertimeTier1AfterHours { get; set; }

    public decimal? OvertimeTier1Rate { get; set; }

    public decimal? OvertimeTier2AfterHours { get; set; }

    public decimal? OvertimeTier2Rate { get; set; }

    public decimal? OvertimeTier3AfterHours { get; set; }

    public decimal? OvertimeTier3Rate { get; set; }

    public string? NightStart { get; set; }

    public string? NightEnd { get; set; }

    public decimal? NightRate { get; set; }

    public decimal? HolidayRate { get; set; }

    public decimal? Weekend1Rate { get; set; }

    public decimal? Weekend2Rate { get; set; }

    public decimal? Weekend3Rate { get; set; }

    public int? VacationDaysPerYear { get; set; }

    public int? MaxWorkDays { get; set; }

    public decimal? MaxOptimalGap { get; set; }

    public decimal? GuaranteedHours { get; set; }

    public decimal? MaximumHours { get; set; }

    public decimal? MinimumHours { get; set; }
}
