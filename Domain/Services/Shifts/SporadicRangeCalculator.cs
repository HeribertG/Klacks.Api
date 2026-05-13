// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
        var monday = d.AddDays(-((int)d.DayOfWeek + 6) % 7);
        return (monday, monday.AddDays(6));
    }

    private static (DateOnly, DateOnly) Month(DateOnly d)
    {
        var first = new DateOnly(d.Year, d.Month, 1);
        return (first, first.AddMonths(1).AddDays(-1));
    }

    private static (DateOnly, DateOnly) Quarter(DateOnly d)
    {
        var qIndex = (d.Month - 1) / 3;
        var first = new DateOnly(d.Year, qIndex * 3 + 1, 1);
        return (first, first.AddMonths(3).AddDays(-1));
    }

    private static (DateOnly, DateOnly) Semester(DateOnly d)
    {
        if (d.Month <= 6)
        {
            return (new DateOnly(d.Year, 1, 1), new DateOnly(d.Year, 6, 30));
        }
        return (new DateOnly(d.Year, 7, 1), new DateOnly(d.Year, 12, 31));
    }

    private static (DateOnly, DateOnly) Year(DateOnly d)
    {
        return (new DateOnly(d.Year, 1, 1), new DateOnly(d.Year, 12, 31));
    }

    private static (DateOnly, DateOnly) ContractualTerm(SporadicShiftInfo shift)
    {
        return (shift.FromDate, shift.UntilDate ?? ContractualTermFallbackEnd);
    }
}
