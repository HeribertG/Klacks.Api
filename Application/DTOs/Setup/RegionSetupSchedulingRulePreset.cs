// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

/// <summary>
/// One named SchedulingRule preset inside an industry profile (K20). Name is required and — combined
/// with the industry slug — forms the import natural key, so renaming a preset in the file creates a
/// NEW rule row rather than updating the old one. All other fields are optional and map 1:1 to the
/// SchedulingRule columns; omitted fields stay null and fall through to the global settings chain.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupSchedulingRulePreset
{
    public string? Name { get; set; }

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

    public decimal? We1Rate { get; set; }

    public decimal? We2Rate { get; set; }

    public decimal? We3Rate { get; set; }

    public string? NightStart { get; set; }

    public string? NightEnd { get; set; }

    public bool? PerformsShiftWork { get; set; }

    public RegionSetupOvertime? Overtime { get; set; }

    public List<RegionSetupSchedulingRuleRateRevision>? RateRevisions { get; set; }
}
