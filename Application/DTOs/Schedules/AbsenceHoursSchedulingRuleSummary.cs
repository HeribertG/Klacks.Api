// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// The hour fields of the scheduling rule attached to the contract the calculation used. A set value wins over the
/// contract's own field (guaranteed hours, full time) or over the settings (daily working hours).
/// </summary>
/// <param name="SchedulingRuleId">Rule id</param>
/// <param name="Name">Rule name</param>
/// <param name="GuaranteedHours">Rule guaranteed hours; null leaves the contract value in place</param>
/// <param name="FullTimeHours">Rule full-time hours; null leaves the contract or settings value in place</param>
/// <param name="DefaultWorkingHours">Rule daily working hours; null leaves the settings value in place</param>
public sealed record AbsenceHoursSchedulingRuleSummary(
    Guid SchedulingRuleId,
    string Name,
    decimal? GuaranteedHours,
    decimal? FullTimeHours,
    decimal? DefaultWorkingHours);
