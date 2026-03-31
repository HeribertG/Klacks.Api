// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Converts Hijri (Islamic) calendar dates to Gregorian using the Tabular Islamic Calendar algorithm.
/// Accuracy: ±1-2 days vs. actual observed dates (sufficient for business planning).
/// </summary>
/// <param name="HijriEpochJdn">Julian Day Number for 1 Muharram 1 AH (July 19, 622 CE proleptic Gregorian)</param>
/// <param name="DaysInHijriCycle">Total days in a 30-year Hijri cycle (10631 days)</param>

namespace Klacks.Api.Domain.Services.Holidays;

public static class HijriCalendar
{
    private const int HijriEpochJdn = 1948440;
    private const int DaysInHijriCycle = 10631;
    private const int YearsInCycle = 30;

    private static readonly int[] LeapYearsInCycle = [2, 5, 7, 10, 13, 16, 18, 21, 24, 26, 29];

    public static DateOnly HijriToGregorian(int hijriYear, int hijriMonth, int hijriDay)
    {
        var jdn = HijriToJdn(hijriYear, hijriMonth, hijriDay);
        return JdnToGregorian(jdn);
    }

    public static (int Year, int Month, int Day) GregorianToHijri(DateOnly date)
    {
        var jdn = GregorianToJdn(date.Year, date.Month, date.Day);
        return JdnToHijri(jdn);
    }

    public static DateOnly GetGregorianDateForHijriInYear(int hijriDay, int hijriMonth, int gregorianYear)
    {
        var (hijriYearStart, _, _) = GregorianToHijri(new DateOnly(gregorianYear, 1, 1));

        for (var candidateYear = hijriYearStart; candidateYear <= hijriYearStart + 2; candidateYear++)
        {
            var dayInMonth = DaysInMonth(candidateYear, hijriMonth);
            var clampedDay = Math.Min(hijriDay, dayInMonth);
            var candidate = HijriToGregorian(candidateYear, hijriMonth, clampedDay);

            if (candidate.Year == gregorianYear)
            {
                return candidate;
            }
        }

        var fallback = HijriToGregorian(hijriYearStart + 1, hijriMonth, hijriDay);
        return fallback;
    }

    private static int HijriToJdn(int year, int month, int day)
    {
        var adjustedYear = year - 1;
        var fullCycles = adjustedYear / YearsInCycle;
        var remainingYears = adjustedYear % YearsInCycle;

        var dayCount = fullCycles * DaysInHijriCycle;

        for (var i = 1; i <= remainingYears; i++)
        {
            dayCount += DaysInHijriYear(i);
        }

        for (var m = 1; m < month; m++)
        {
            dayCount += (m % 2 == 1) ? 30 : 29;
        }

        dayCount += day;

        return dayCount + HijriEpochJdn - 1;
    }

    private static (int Year, int Month, int Day) JdnToHijri(int jdn)
    {
        var daysSinceEpoch = jdn - HijriEpochJdn + 1;

        var fullCycles = (daysSinceEpoch - 1) / DaysInHijriCycle;
        var remainingDays = daysSinceEpoch - (fullCycles * DaysInHijriCycle);

        var year = (fullCycles * YearsInCycle) + 1;

        while (remainingDays > DaysInHijriYear(((year - 1) % YearsInCycle) + 1))
        {
            remainingDays -= DaysInHijriYear(((year - 1) % YearsInCycle) + 1);
            year++;
        }

        var month = 1;
        while (true)
        {
            var daysInCurrentMonth = (month % 2 == 1) ? 30 : 29;
            if (month == 12 && IsHijriLeapYear(((year - 1) % YearsInCycle) + 1))
            {
                daysInCurrentMonth = 30;
            }

            if (remainingDays <= daysInCurrentMonth)
            {
                break;
            }

            remainingDays -= daysInCurrentMonth;
            month++;
        }

        return (year, month, remainingDays);
    }

    private static int DaysInHijriYear(int yearInCycle)
    {
        return IsHijriLeapYear(yearInCycle) ? 355 : 354;
    }

    private static bool IsHijriLeapYear(int yearInCycle)
    {
        return Array.Exists(LeapYearsInCycle, y => y == yearInCycle);
    }

    private static int DaysInMonth(int hijriYear, int month)
    {
        if (month == 12 && IsHijriLeapYear(((hijriYear - 1) % YearsInCycle) + 1))
        {
            return 30;
        }

        return (month % 2 == 1) ? 30 : 29;
    }

    private static int GregorianToJdn(int year, int month, int day)
    {
        var a = (14 - month) / 12;
        var y = year + 4800 - a;
        var m = month + (12 * a) - 3;

        return day + ((153 * m + 2) / 5) + (365 * y) + (y / 4) - (y / 100) + (y / 400) - 32045;
    }

    private static DateOnly JdnToGregorian(int jdn)
    {
        var a = jdn + 32044;
        var b = ((4 * a) + 3) / 146097;
        var c = a - ((146097 * b) / 4);
        var d = ((4 * c) + 3) / 1461;
        var e = c - ((1461 * d) / 4);
        var m = ((5 * e) + 2) / 153;

        var day = e - ((153 * m + 2) / 5) + 1;
        var month = m + 3 - (12 * (m / 10));
        var year = (100 * b) + d - 4800 + (m / 10);

        return new DateOnly(year, month, day);
    }
}
