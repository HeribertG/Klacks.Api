// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Derives the first day of the pay period that ends on a given day. PeriodCloseDueDetector and
/// PeriodOverdueDetector each compute their own period END from the group's PaymentInterval; this
/// adds the matching START, which is what a range query over the period needs. Individual is not
/// supported here for the same reason both detectors skip it: it has no derivable cycle.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public static class PeriodBoundaries
{
    private const int WeeklyPeriodDays = 7;
    private const int BiweeklyPeriodDays = 14;

    public static DateOnly StartFor(PaymentInterval interval, DateOnly periodEnd) =>
        interval switch
        {
            PaymentInterval.Weekly => periodEnd.AddDays(-(WeeklyPeriodDays - 1)),
            PaymentInterval.Biweekly => periodEnd.AddDays(-(BiweeklyPeriodDays - 1)),
            PaymentInterval.Monthly => new DateOnly(periodEnd.Year, periodEnd.Month, 1),
            PaymentInterval.MonthlyTargetHours => new DateOnly(periodEnd.Year, periodEnd.Month, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(interval),
                $"Unsupported PaymentInterval '{interval}' — caller must filter Individual.")
        };
}
