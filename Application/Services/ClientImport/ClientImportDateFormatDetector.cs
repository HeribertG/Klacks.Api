// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides the day/month order of the date columns of an import. The order is only called
/// unambiguous when the data proves it: a first number above 12 proves day-month-year, a second number
/// above 12 proves month-day-year, a four-digit first number proves year-month-day. Contradicting or
/// missing evidence leaves it ambiguous (default day-month-year) so the user must choose. Values with a
/// four-digit year first (ISO, real xlsx date cells) are read the same in every order and prove nothing
/// about the others.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.ClientImport;

public static class ClientImportDateFormatDetector
{
    private const int MaxMonth = 12;
    private const int FourDigitYearLength = 4;

    public static (ClientImportDateFormat Format, bool Ambiguous) Detect(IEnumerable<string> values)
    {
        var dayFirst = false;
        var monthFirst = false;
        var sawNonIso = false;

        foreach (var value in values)
        {
            if (!ClientImportDateParser.TrySplit(value, out var first, out var second, out _))
            {
                continue;
            }

            if (first.Length == FourDigitYearLength)
            {
                continue;
            }

            sawNonIso = true;
            var firstNumber = int.Parse(first, NumberStyles.None, CultureInfo.InvariantCulture);
            var secondNumber = int.Parse(second, NumberStyles.None, CultureInfo.InvariantCulture);

            if (firstNumber > MaxMonth)
            {
                dayFirst = true;
            }

            if (secondNumber > MaxMonth)
            {
                monthFirst = true;
            }
        }

        if (!sawNonIso)
        {
            return (ClientImportDateFormat.DayMonthYear, false);
        }

        if (dayFirst && !monthFirst)
        {
            return (ClientImportDateFormat.DayMonthYear, false);
        }

        if (monthFirst && !dayFirst)
        {
            return (ClientImportDateFormat.MonthDayYear, false);
        }

        return (ClientImportDateFormat.DayMonthYear, true);
    }
}
