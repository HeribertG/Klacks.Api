// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Scales a contract's pay-period target (GuaranteedHours, stated per PaymentInterval) down to an arbitrary
/// planning range. Each calendar day of the range contributes GuaranteedHours / length of the pay period that
/// contains the day, so a full pay period yields exactly GuaranteedHours, a 7-day slice of a 31-day month yields
/// 7/31 of it, and a range crossing a month boundary takes each day's share from its own month (including a
/// different monthly value). Pure calendar arithmetic on DateOnly, no time zone involved.
/// </summary>
/// <param name="interval">PaymentInterval of the contract (or of the settings fallback for clients without one)</param>
/// <param name="date">Calendar day whose pay period is measured</param>
/// <param name="individualPeriods">Period rows of the contract's IndividualPeriod; only read for PaymentInterval.Individual</param>
/// <param name="guaranteedHours">Target hours of the full pay period that contains the day</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public static class PayPeriodTargetHoursProrator
{
    public const int WeeklyPeriodDays = 7;
    public const int BiweeklyPeriodDays = 14;

    private const string UnnamedIndividualPeriod = "individual";

    /// <summary>
    /// Number of calendar days of the pay period that contains <paramref name="date"/>. Monthly and
    /// MonthlyTargetHours use the calendar month. Individual uses the contract's own period rows; when they are
    /// missing or do not cover the day, the calendar month is used. That matches PeriodHoursService's fallback for a
    /// global Individual setting; its per-contract path throws instead, which a planning run must not do.
    /// </summary>
    public static int PayPeriodLengthDays(
        PaymentInterval interval,
        DateOnly date,
        IReadOnlyCollection<Period>? individualPeriods = null)
    {
        return interval switch
        {
            PaymentInterval.Weekly => WeeklyPeriodDays,
            PaymentInterval.Biweekly => BiweeklyPeriodDays,
            PaymentInterval.Individual => IndividualPeriodLengthDays(date, individualPeriods),
            _ => DateTime.DaysInMonth(date.Year, date.Month),
        };
    }

    /// <summary>Share of the pay-period target that one calendar day carries.</summary>
    public static decimal DailyShare(decimal guaranteedHours, int payPeriodLengthDays)
    {
        if (payPeriodLengthDays <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payPeriodLengthDays), "The pay period must span at least one day.");
        }

        return guaranteedHours / payPeriodLengthDays;
    }

    /// <summary>Daily share of <paramref name="guaranteedHours"/> for the pay period containing <paramref name="date"/>.</summary>
    public static decimal DailyShare(
        decimal guaranteedHours,
        PaymentInterval interval,
        DateOnly date,
        IReadOnlyCollection<Period>? individualPeriods = null)
        => DailyShare(guaranteedHours, PayPeriodLengthDays(interval, date, individualPeriods));

    private static int IndividualPeriodLengthDays(DateOnly date, IReadOnlyCollection<Period>? periods)
    {
        if (periods is null || periods.Count == 0)
        {
            return DateTime.DaysInMonth(date.Year, date.Month);
        }

        try
        {
            var (start, end) = IndividualPeriodBoundaryResolver.Resolve(periods, date, UnnamedIndividualPeriod);
            return end.DayNumber - start.DayNumber + 1;
        }
        catch (InvalidRequestException)
        {
            return DateTime.DaysInMonth(date.Year, date.Month);
        }
    }
}
