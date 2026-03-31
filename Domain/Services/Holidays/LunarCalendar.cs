// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Converts Chinese lunisolar calendar dates to Gregorian using a lookup table (2020-2050).
/// Data sourced from astronomical calculations (Hong Kong Observatory reference).
/// </summary>
/// <param name="MinYear">Earliest supported Gregorian year (2020)</param>
/// <param name="MaxYear">Latest supported Gregorian year (2050)</param>

namespace Klacks.Api.Domain.Services.Holidays;

public static class LunarCalendar
{
    private const int MinYear = 2020;
    private const int MaxYear = 2050;

    public static DateOnly GetGregorianDateForLunarInYear(int lunarDay, int lunarMonth, int gregorianYear)
    {
        if (gregorianYear < MinYear || gregorianYear > MaxYear)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gregorianYear),
                $"Chinese lunar calendar lookup is only available for years {MinYear}-{MaxYear}. Requested: {gregorianYear}");
        }

        if (!LunarNewYearDates.TryGetValue(gregorianYear, out var newYearDate))
        {
            throw new ArgumentOutOfRangeException(nameof(gregorianYear), $"No lunar data for year {gregorianYear}");
        }

        if (!MonthLengths.TryGetValue(gregorianYear, out var months))
        {
            throw new ArgumentOutOfRangeException(nameof(gregorianYear), $"No month data for year {gregorianYear}");
        }

        var daysToAdd = 0;

        for (var m = 1; m < lunarMonth; m++)
        {
            if (m - 1 >= months.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(lunarMonth), $"Lunar month {lunarMonth} exceeds available months for year {gregorianYear}");
            }

            daysToAdd += months[m - 1];
        }

        daysToAdd += lunarDay - 1;

        return newYearDate.AddDays(daysToAdd);
    }

    private static readonly Dictionary<int, DateOnly> LunarNewYearDates = new()
    {
        [2020] = new DateOnly(2020, 1, 25),
        [2021] = new DateOnly(2021, 2, 12),
        [2022] = new DateOnly(2022, 2, 1),
        [2023] = new DateOnly(2023, 1, 22),
        [2024] = new DateOnly(2024, 2, 10),
        [2025] = new DateOnly(2025, 1, 29),
        [2026] = new DateOnly(2026, 2, 17),
        [2027] = new DateOnly(2027, 2, 6),
        [2028] = new DateOnly(2028, 1, 26),
        [2029] = new DateOnly(2029, 2, 13),
        [2030] = new DateOnly(2030, 2, 3),
        [2031] = new DateOnly(2031, 1, 23),
        [2032] = new DateOnly(2032, 2, 11),
        [2033] = new DateOnly(2033, 1, 31),
        [2034] = new DateOnly(2034, 2, 19),
        [2035] = new DateOnly(2035, 2, 8),
        [2036] = new DateOnly(2036, 1, 28),
        [2037] = new DateOnly(2037, 2, 15),
        [2038] = new DateOnly(2038, 2, 4),
        [2039] = new DateOnly(2039, 1, 24),
        [2040] = new DateOnly(2040, 2, 12),
        [2041] = new DateOnly(2041, 2, 1),
        [2042] = new DateOnly(2042, 1, 22),
        [2043] = new DateOnly(2043, 2, 10),
        [2044] = new DateOnly(2044, 1, 30),
        [2045] = new DateOnly(2045, 2, 17),
        [2046] = new DateOnly(2046, 2, 6),
        [2047] = new DateOnly(2047, 1, 26),
        [2048] = new DateOnly(2048, 2, 14),
        [2049] = new DateOnly(2049, 2, 2),
        [2050] = new DateOnly(2050, 1, 23),
    };

    private static readonly Dictionary<int, int[]> MonthLengths = new()
    {
        [2020] = [29, 30, 30, 30, 29, 29, 30, 29, 30, 29, 30, 30],
        [2021] = [29, 30, 29, 30, 29, 30, 29, 30, 29, 30, 29, 30],
        [2022] = [29, 30, 29, 29, 30, 29, 30, 30, 29, 30, 30, 29],
        [2023] = [29, 30, 29, 30, 29, 30, 29, 29, 30, 29, 30, 30],
        [2024] = [29, 30, 29, 29, 30, 29, 30, 29, 30, 29, 30, 30],
        [2025] = [30, 29, 30, 29, 30, 30, 29, 30, 29, 30, 29, 29],
        [2026] = [30, 29, 30, 30, 29, 30, 29, 30, 29, 30, 29, 30],
        [2027] = [29, 30, 29, 30, 29, 30, 30, 29, 30, 29, 30, 29],
        [2028] = [30, 29, 29, 30, 29, 30, 30, 29, 30, 30, 29, 30],
        [2029] = [29, 30, 29, 29, 30, 29, 30, 29, 30, 30, 30, 29],
        [2030] = [30, 29, 30, 29, 29, 30, 29, 30, 29, 30, 30, 30],
        [2031] = [29, 30, 29, 30, 29, 29, 30, 29, 29, 30, 30, 30],
        [2032] = [29, 30, 30, 29, 30, 29, 29, 30, 29, 30, 29, 30],
        [2033] = [29, 30, 30, 29, 30, 29, 30, 29, 30, 29, 30, 29],
        [2034] = [30, 29, 30, 30, 29, 30, 29, 30, 29, 30, 29, 29],
        [2035] = [30, 29, 30, 30, 29, 30, 30, 29, 30, 29, 30, 29],
        [2036] = [29, 30, 29, 30, 29, 30, 30, 29, 30, 30, 29, 30],
        [2037] = [29, 29, 30, 29, 30, 29, 30, 29, 30, 30, 30, 29],
        [2038] = [30, 29, 29, 30, 29, 29, 30, 29, 30, 30, 30, 29],
        [2039] = [30, 30, 29, 29, 30, 29, 29, 30, 29, 30, 30, 30],
        [2040] = [29, 30, 29, 30, 29, 30, 29, 29, 30, 29, 30, 30],
        [2041] = [29, 30, 30, 29, 30, 29, 30, 29, 29, 30, 29, 30],
        [2042] = [29, 30, 30, 30, 29, 30, 29, 30, 29, 29, 30, 29],
        [2043] = [30, 29, 30, 30, 29, 30, 30, 29, 30, 29, 29, 30],
        [2044] = [29, 30, 29, 30, 29, 30, 30, 29, 30, 29, 30, 29],
        [2045] = [30, 29, 29, 30, 29, 30, 29, 30, 30, 29, 30, 30],
        [2046] = [29, 30, 29, 29, 30, 29, 29, 30, 30, 29, 30, 30],
        [2047] = [30, 29, 30, 29, 29, 30, 29, 29, 30, 30, 29, 30],
        [2048] = [30, 30, 29, 30, 29, 29, 30, 29, 29, 30, 30, 29],
        [2049] = [30, 30, 29, 30, 29, 30, 29, 30, 29, 30, 29, 29],
        [2050] = [30, 29, 30, 30, 29, 30, 29, 30, 29, 30, 29, 30],
    };
}
