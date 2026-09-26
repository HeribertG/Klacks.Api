// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure rule behind the period-close schedule: a period is closed on its last day plus the configured lag,
/// never before the period has ended. Individual groups have no derivable cycle and therefore no close date.
/// No I/O, no clock; the caller supplies today so the result is independent of any time zone.
/// </summary>
/// <param name="interval">Payment interval of the group the period belongs to</param>
/// <param name="periodEnd">Last day of the period</param>
/// <param name="lagDays">Days after the period end on which the period is closed</param>
/// <param name="today">The company's own local day</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Assistant;

public static class PeriodCloseDateCalculator
{
    public const int MinLagDays = 0;
    public const int MaxLagDays = 31;

    public static bool IsValidLag(int lagDays)
    {
        return lagDays >= MinLagDays && lagDays <= MaxLagDays;
    }

    public static DateOnly? CloseDateFor(PaymentInterval interval, DateOnly periodEnd, int lagDays)
    {
        if (!NextPeriodBoundaries.HasDerivableCycle(interval) || !IsValidLag(lagDays))
        {
            return null;
        }

        return periodEnd.AddDays(lagDays);
    }

    public static bool IsAutoCloseDue(DateOnly today, DateOnly periodEnd, int lagDays)
    {
        if (!IsValidLag(lagDays))
        {
            return false;
        }

        return today > periodEnd && today >= periodEnd.AddDays(lagDays);
    }

    /// <summary>
    /// The first day an automatic close of the period is allowed: the close date, but never earlier than the
    /// day after the period end (a lag of 0 still waits until the period is over).
    /// </summary>
    public static DateOnly FirstAutoCloseDay(DateOnly periodEnd, int lagDays)
    {
        return periodEnd.AddDays(Math.Max(lagDays, 1));
    }

    /// <summary>
    /// The exclusive bound for the latest period end that is due today: every period end strictly before this
    /// day satisfies IsAutoCloseDue, the one on or after it does not.
    /// </summary>
    public static DateOnly DuePeriodEndBound(DateOnly today, int lagDays)
    {
        return today.AddDays(1 - Math.Max(lagDays, 1));
    }

    /// <summary>
    /// Whether today is still inside the window in which an automatic close may run, counted from
    /// FirstAutoCloseDay. Outside of it the period is left to a person.
    /// </summary>
    public static bool IsWithinAutoCloseWindow(DateOnly today, DateOnly periodEnd, int lagDays, int windowDays)
    {
        var firstDay = FirstAutoCloseDay(periodEnd, lagDays);
        return today >= firstDay && today.DayNumber - firstDay.DayNumber <= windowDays;
    }
}
