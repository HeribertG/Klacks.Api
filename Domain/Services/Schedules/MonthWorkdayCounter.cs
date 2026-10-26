// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Counts the workdays of a calendar month: every day that is neither one of the company's configured weekend
/// days nor a day the caller's holiday rule marks as a holiday. The holiday rule is passed in so the caller reuses
/// the calendar the absence macros use (official holidays of the employee's calendar) instead of a second one.
/// </summary>
/// <param name="year">Calendar year</param>
/// <param name="month">Calendar month (1-12)</param>
/// <param name="weekendDays">The company's configured weekend days</param>
/// <param name="isHoliday">The holiday rule of the applicable calendar; null when the installation has no calendar</param>

namespace Klacks.Api.Domain.Services.Schedules;

public static class MonthWorkdayCounter
{
    public static int Count(int year, int month, IReadOnlySet<DayOfWeek> weekendDays, Func<DateOnly, bool>? isHoliday)
    {
        var workdays = 0;
        var daysInMonth = DateTime.DaysInMonth(year, month);
        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(year, month, day);
            if (weekendDays.Contains(date.DayOfWeek) || (isHoliday != null && isHoliday(date)))
            {
                continue;
            }

            workdays++;
        }

        return workdays;
    }
}
