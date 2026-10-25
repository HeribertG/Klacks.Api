// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// The company-wide work settings the absence-hours calculation falls back to when neither a contract nor a
/// scheduling rule sets a value.
/// </summary>
/// <param name="GuaranteedHours">Settings guaranteed hours per company payment interval</param>
/// <param name="FullTime">Settings full-time hours</param>
/// <param name="DefaultWorkingHours">Settings daily working hours - the "Hour" every absence macro starts from</param>
/// <param name="PaymentInterval">Company payment interval the Ui books absences with when no group interval applies; null when none is configured (no filter)</param>
public sealed record AbsenceHoursSettingsSummary(
    decimal GuaranteedHours,
    decimal FullTime,
    decimal DefaultWorkingHours,
    int? PaymentInterval);
