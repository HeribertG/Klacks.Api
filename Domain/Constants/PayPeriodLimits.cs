// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Upper bound for splitting a date range into pay periods, so a caller-chosen range (the RecalculatePeriodHours endpoint,
/// a thorough recalculation window) cannot trigger an unbounded number of recalculations. 600 periods cover more than
/// eleven years of weekly pay periods.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class PayPeriodLimits
{
    public const int MaxPeriodsInRange = 600;
}
