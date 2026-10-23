// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared Gregorian calendar unit sizes (days per week and fortnight, months per quarter, semester and year), so calendar
/// arithmetic names its units instead of repeating bare 7/3/6/12 literals.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class CalendarConstants
{
    public const int DaysPerWeek = 7;

    public const int DaysPerFortnight = DaysPerWeek * 2;

    public const int MonthsPerQuarter = 3;

    public const int MonthsPerSemester = 6;

    public const int MonthsPerYear = 12;
}
