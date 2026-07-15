// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Setup;

/// <summary>
/// Value payload of one desired SchedulingRule preset row for the region-setup entity import (K20).
/// Field order mirrors the SchedulingRule columns; every field participates in the content hash.
/// Keep the three sets in sync when adding a field: this record, ComputeSchedulingRulePresetContentHash
/// and CopyPresetValues/ToImportValues in RegionSetupService — a field present in only two of the three
/// silently breaks the customer-edit detection.
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
    bool? PerformsShiftWork,
    OvertimeBasis? OvertimeBasis,
    SurchargeRateMode? OvertimeRateMode,
    decimal? OvertimeTier1AfterHours,
    decimal? OvertimeTier1Rate,
    decimal? OvertimeTier2AfterHours,
    decimal? OvertimeTier2Rate,
    decimal? OvertimeTier3AfterHours,
    decimal? OvertimeTier3Rate);
