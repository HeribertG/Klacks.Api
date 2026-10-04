// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Setup;

/// <summary>
/// Value payload of one desired SchedulingRule preset row for the region-setup entity import (K20).
/// Field order mirrors the SchedulingRule columns; every field except Industry participates in the
/// content hash. Industry is classification derived from the block's industry slug, not editable
/// content: it is re-applied on Insert AND Update but deliberately excluded from
/// SchedulingRulePresetContentHasher, so rows written by an older binary (Industry empty) are
/// never misread as customer-edited. Keep the three sets in sync when adding a hashed field: this
/// record, SchedulingRulePresetContentHasher and CopyPresetValues/ToImportValues in
/// RegionSetupService — a field present in only two of the three silently breaks the customer-edit
/// detection. MaxDailySpanHours is appended last and hashed only when set, so presets written before the field
/// existed keep their stored hash.
/// </summary>
public sealed record SchedulingRulePresetImportValues(
    string Name,
    int? MaxWorkDays,
    decimal? MinRestDays,
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
    decimal? OvertimeTier3Rate,
    string Industry,
    decimal? MaxDailySpanHours = null);
