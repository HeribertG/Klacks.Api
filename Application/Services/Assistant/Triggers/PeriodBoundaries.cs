// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Derives pay-period boundaries from a group's PaymentInterval. StartFor gives the first day of the period
/// that ends on a given day, which is what a range query over the period needs. LastEndBefore gives the end
/// of the latest period that ended strictly before a reference day; PeriodOverdueDetector and the autonomous
/// period close share it, so both always talk about the same period. Individual is not supported here for
/// the same reason every caller skips it: it has no derivable cycle.
/// </summary>
/// <param name="interval">Payment interval of the group</param>
/// <param name="periodEnd">Last day of the period whose start is wanted</param>
/// <param name="group">Supplies the PaymentInterval and, for biweekly cycles, the ValidFrom anchor</param>
/// <param name="referenceDay">Day before which the period must have ended</param>
/// <param name="referenceWeekStart">First day of the configured week that contains the reference day</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;

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

    public static DateOnly LastEndBefore(Group group, DateOnly referenceDay, DateOnly referenceWeekStart) =>
        group.PaymentInterval switch
        {
            PaymentInterval.Weekly => referenceWeekStart.AddDays(-1),
            PaymentInterval.Biweekly => LastBiweeklyEnd(referenceDay, group.ValidFrom),
            PaymentInterval.Monthly => LastMonthEnd(referenceDay),
            PaymentInterval.MonthlyTargetHours => LastMonthEnd(referenceDay),
            _ => throw new ArgumentOutOfRangeException(nameof(group),
                $"Unsupported PaymentInterval '{group.PaymentInterval}' — caller must filter Individual.")
        };

    private static DateOnly LastMonthEnd(DateOnly referenceDay)
    {
        return new DateOnly(referenceDay.Year, referenceDay.Month, 1).AddDays(-1);
    }

    private static DateOnly LastBiweeklyEnd(DateOnly referenceDay, DateTime groupAnchor)
    {
        var anchor = DateOnly.FromDateTime(groupAnchor);
        var daysSinceAnchor = referenceDay.DayNumber - anchor.DayNumber;
        var positionInCycle = ((daysSinceAnchor % BiweeklyPeriodDays) + BiweeklyPeriodDays) % BiweeklyPeriodDays;
        return referenceDay.AddDays(BiweeklyPeriodDays - 1 - positionInCycle).AddDays(-BiweeklyPeriodDays);
    }
}
