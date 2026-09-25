// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Boundaries of a group's NEXT pay-period, derived from its PaymentInterval. Shared by the next-period
/// detector and the planning-deadline skill so both always talk about the same period. Individual has no
/// derivable cycle and is rejected: callers filter it with HasDerivableCycle first.
/// </summary>
/// <param name="group">Supplies the PaymentInterval and, for biweekly cycles, the ValidFrom anchor</param>
/// <param name="today">The company's own local day</param>
/// <param name="nextWeekStart">First day of the week after the current one, per the configured week start</param>
/// <param name="periodStart">Start of the period whose end is wanted</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Services.Assistant;

public static class NextPeriodBoundaries
{
    public const int WeeklyPeriodDays = 7;
    public const int BiweeklyCycleDays = 14;

    public static bool HasDerivableCycle(PaymentInterval interval)
    {
        return interval != PaymentInterval.Individual;
    }

    public static DateOnly ComputeStart(Group group, DateOnly today, DateOnly nextWeekStart)
    {
        return group.PaymentInterval switch
        {
            PaymentInterval.Weekly => nextWeekStart,
            PaymentInterval.Biweekly => EndOfBiweekly(today, group.ValidFrom).AddDays(1),
            PaymentInterval.Monthly => FirstOfNextMonth(today),
            PaymentInterval.MonthlyTargetHours => FirstOfNextMonth(today),
            _ => throw new ArgumentOutOfRangeException(nameof(group),
                $"Unsupported PaymentInterval '{group.PaymentInterval}' — caller must filter Individual.")
        };
    }

    public static DateOnly ComputeEnd(Group group, DateOnly periodStart)
    {
        return group.PaymentInterval switch
        {
            PaymentInterval.Weekly => periodStart.AddDays(WeeklyPeriodDays - 1),
            PaymentInterval.Biweekly => periodStart.AddDays(BiweeklyCycleDays - 1),
            PaymentInterval.Monthly => EndOfMonth(periodStart),
            PaymentInterval.MonthlyTargetHours => EndOfMonth(periodStart),
            _ => throw new ArgumentOutOfRangeException(nameof(group),
                $"Unsupported PaymentInterval '{group.PaymentInterval}' — caller must filter Individual.")
        };
    }

    private static DateOnly FirstOfNextMonth(DateOnly today)
    {
        return new DateOnly(today.Year, today.Month, 1).AddMonths(1);
    }

    private static DateOnly EndOfMonth(DateOnly date)
    {
        var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
        return new DateOnly(date.Year, date.Month, daysInMonth);
    }

    private static DateOnly EndOfBiweekly(DateOnly today, DateTime groupAnchor)
    {
        var anchor = DateOnly.FromDateTime(groupAnchor);
        var daysSinceAnchor = today.DayNumber - anchor.DayNumber;
        var positionInCycle = ((daysSinceAnchor % BiweeklyCycleDays) + BiweeklyCycleDays) % BiweeklyCycleDays;
        return today.AddDays(BiweeklyCycleDays - 1 - positionInCycle);
    }
}
