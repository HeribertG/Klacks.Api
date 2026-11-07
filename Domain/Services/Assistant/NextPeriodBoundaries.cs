// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Boundaries of a group's NEXT pay-period, derived from its PaymentInterval. Shared by the next-period
/// detector and the planning-deadline skill so both always talk about the same period. Individual has no
/// derivable cycle and is rejected: callers filter it with HasDerivableCycle first. Biweekly periods are the
/// installation-wide pay periods of BiweeklyPayPeriod (fixed anchor, configured week start), the same ones
/// PeriodHoursService uses; until 2026-11 they were anchored on group.ValidFrom, the group's validity date.
/// </summary>
/// <param name="group">Supplies the PaymentInterval</param>
/// <param name="today">The company's own local day</param>
/// <param name="nextWeekStart">First day of the week after the current one, per the configured week start</param>
/// <param name="periodStart">Start of the period whose end is wanted</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Domain.Services.Assistant;

public static class NextPeriodBoundaries
{
    public const int WeeklyPeriodDays = 7;

    public static bool HasDerivableCycle(PaymentInterval interval)
    {
        return interval != PaymentInterval.Individual;
    }

    public static DateOnly ComputeStart(Group group, DateOnly today, DateOnly nextWeekStart)
    {
        return group.PaymentInterval switch
        {
            PaymentInterval.Weekly => nextWeekStart,
            PaymentInterval.Biweekly => BiweeklyPayPeriod.Containing(today, nextWeekStart.DayOfWeek).End.AddDays(1),
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
            PaymentInterval.Biweekly => periodStart.AddDays(BiweeklyPayPeriod.CycleDays - 1),
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
}
