// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Setup;

/// <summary>
/// Value payload of one desired SchedulingRule preset row for the region-setup entity import (K20).
/// Field order mirrors the SchedulingRule columns; every field participates in the content hash.
/// </summary>
public sealed record SchedulingRulePresetImportValues(
    string Name,
    int? MaxWorkDays,
    int? MinRestDays,
    decimal? MinPauseHours,
    decimal? MaxOptimalGap,
    decimal? MaxDailyHours,
    decimal? MaxWeeklyHours,
    int? MaxConsecutiveDays,
    decimal? DefaultWorkingHours,
    decimal? OvertimeThreshold,
    decimal? GuaranteedHours,
    decimal? MaximumHours,
    decimal? MinimumHours,
    decimal? FullTimeHours,
    int? VacationDaysPerYear,
    decimal? NightRate,
    decimal? HolidayRate,
    decimal? We1Rate,
    decimal? We2Rate,
    decimal? We3Rate,
    string? NightStart,
    string? NightEnd,
    bool? PerformsShiftWork);
