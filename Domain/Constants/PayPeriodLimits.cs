// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Limits around pay periods. MaxPeriodsInRange bounds splitting a date range into pay periods, so a caller-chosen range
/// (the RecalculatePeriodHours endpoint, a thorough recalculation window) cannot trigger an unbounded number of
/// recalculations; 600 periods cover more than eleven years of weekly pay periods. RecalculableExportRanges is the number
/// of a person's latest distinct payroll export ranges whose periods stay recalculable; an older period that is sealed
/// and exported is frozen (ExportedPeriodWindow, IFrozenPeriodPolicy). MaxPeriodsForPlannerRecalculation caps the pay
/// periods a non-admin may recalculate by hand in one request (ManualRecalculationWindow); the admin keeps
/// MaxPeriodsInRange.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class PayPeriodLimits
{
    public const int MaxPeriodsInRange = 600;

    public const int RecalculableExportRanges = 2;

    public const int MaxPeriodsForPlannerRecalculation = 12;
}
