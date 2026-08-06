// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Scheduling;

public class SchedulingRuleResource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? MaxWorkDays { get; set; }

    public decimal? MinRestDays { get; set; }

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

    [JsonPropertyName("we1Rate")]
    public decimal? WE1Rate { get; set; }

    [JsonPropertyName("we2Rate")]
    public decimal? WE2Rate { get; set; }

    [JsonPropertyName("we3Rate")]
    public decimal? WE3Rate { get; set; }

    public string? NightStart { get; set; }

    public string? NightEnd { get; set; }

    public bool? WorkOnMonday { get; set; }

    public bool? WorkOnTuesday { get; set; }

    public bool? WorkOnWednesday { get; set; }

    public bool? WorkOnThursday { get; set; }

    public bool? WorkOnFriday { get; set; }

    public bool? WorkOnSaturday { get; set; }

    public bool? WorkOnSunday { get; set; }

    public bool? PerformsShiftWork { get; set; }

    /// <summary>
    /// Industry slug this rule was imported for, empty for a customer-owned rule. Read-only: it is
    /// import identity, not an editable field, and is ignored on both create and update so an edit
    /// through the UI can never clear the tag and drop the rule out of the industry filter.
    /// </summary>
    public string Industry { get; set; } = string.Empty;

    /// <summary>
    /// Set when the rule originates from a region package, empty for a customer-owned rule. Read-only
    /// for the same reason as <see cref="Industry"/>.
    /// </summary>
    public string ImportSourceKey { get; set; } = string.Empty;
}
