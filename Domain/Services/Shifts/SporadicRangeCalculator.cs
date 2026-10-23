// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Shifts;

/// <summary>
/// Pure calculator that derives the blocking range of a sporadic shift booking from its scope.
/// </summary>
public static class SporadicRangeCalculator
{
    private static readonly DateOnly ContractualTermFallbackEnd = new(9999, 12, 31);

    /// <summary>
    /// Computes the (from, until) range that a single sporadic booking at <paramref name="bookingDate"/> blocks.
    /// </summary>
    /// <param name="shift">The slim shift projection. <c>FromDate</c> / <c>UntilDate</c> are used for <see cref="ShiftSporadic.ContractualTerm"/>.</param>
    /// <param name="bookingDate">The date on which the work was booked.</param>
    public static (DateOnly From, DateOnly Until) Compute(SporadicShiftInfo shift, DateOnly bookingDate)
    {
        return shift.SporadicScope switch
        {
            ShiftSporadic.Week => Week(bookingDate),
            ShiftSporadic.Month => Month(bookingDate),
            ShiftSporadic.Quarter => Quarter(bookingDate),
            ShiftSporadic.Semester => Semester(bookingDate),
            ShiftSporadic.Year => Year(bookingDate),
            ShiftSporadic.ContractualTerm => ContractualTerm(shift),
            _ => (bookingDate, bookingDate)
        };
    }

    private static (DateOnly, DateOnly) Week(DateOnly d)
    {
        var monday = d.AddDays(-((int)d.DayOfWeek + CalendarConstants.DaysPerWeek - 1) % CalendarConstants.DaysPerWeek);
        return (monday, monday.AddDays(CalendarConstants.DaysPerWeek - 1));
    }

    private static (DateOnly, DateOnly) Month(DateOnly d)
    {
        var first = new DateOnly(d.Year, d.Month, 1);
        return (first, first.AddMonths(1).AddDays(-1));
    }

    private static (DateOnly, DateOnly) Quarter(DateOnly d) => MonthBlock(d, CalendarConstants.MonthsPerQuarter);

    private static (DateOnly, DateOnly) Semester(DateOnly d) => MonthBlock(d, CalendarConstants.MonthsPerSemester);

    private static (DateOnly, DateOnly) Year(DateOnly d) => MonthBlock(d, CalendarConstants.MonthsPerYear);

    private static (DateOnly, DateOnly) MonthBlock(DateOnly d, int monthsPerBlock)
    {
        var firstMonth = (d.Month - 1) / monthsPerBlock * monthsPerBlock + 1;
        var lastMonth = firstMonth + monthsPerBlock - 1;
        return (new DateOnly(d.Year, firstMonth, 1), new DateOnly(d.Year, lastMonth, DateTime.DaysInMonth(d.Year, lastMonth)));
    }

    private static (DateOnly, DateOnly) ContractualTerm(SporadicShiftInfo shift)
    {
        return (shift.FromDate, shift.UntilDate ?? ContractualTermFallbackEnd);
    }
}
