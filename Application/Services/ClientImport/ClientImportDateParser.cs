// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Parses import date cells without guessing: ISO (yyyy-MM-dd, which the xlsx reader emits for real date
/// cells) is always accepted; every other value is split into three numbers and read in the order the
/// user confirmed (day-month-year, month-day-year or year-month-day). A two-digit year is expanded with
/// a pivot on the company's current year (not later than this year's two digits -> 20xx, else 19xx) and
/// reported so the caller can warn. The result is always a UTC-midnight DateTime (Kind=Utc).
/// </summary>

using System.Globalization;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.ClientImport;

public static class ClientImportDateParser
{
    public const string IsoFormat = "yyyy-MM-dd";

    private const int CenturyDivisor = 100;
    private const int TwoDigitYearLength = 2;
    private const int FourDigitYearLength = 4;
    private const int TwentiethCentury = 1900;
    private const int TwentyFirstCentury = 2000;

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

    private static readonly Regex DatePattern = new(
        @"^\s*(?<a>\d{1,4})\s*[./\-]\s*(?<b>\d{1,2})\s*[./\-]\s*(?<c>\d{1,4})\.?(?:[ T]\d{1,2}:\d{2}(?::\d{2})?(?:\.\d+)?)?\s*$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        RegexTimeout);

    public static bool TryParse(
        string? value,
        ClientImportDateFormat format,
        int currentYear,
        out DateTime result,
        out bool twoDigitYear)
    {
        result = default;
        twoDigitYear = false;

        if (!TrySplit(value, out var first, out var second, out var third))
        {
            return false;
        }

        string year;
        string month;
        string day;

        if (first.Length == FourDigitYearLength)
        {
            (year, month, day) = (first, second, third);
        }
        else
        {
            (year, month, day) = format switch
            {
                ClientImportDateFormat.MonthDayYear => (third, first, second),
                ClientImportDateFormat.YearMonthDay => (first, second, third),
                _ => (third, second, first)
            };
        }

        if (year.Length == TwoDigitYearLength)
        {
            year = ExpandTwoDigitYear(int.Parse(year, NumberStyles.None, CultureInfo.InvariantCulture), currentYear)
                .ToString(CultureInfo.InvariantCulture);
            twoDigitYear = true;
        }

        if (year.Length != FourDigitYearLength || day.Length > TwoDigitYearLength || month.Length > TwoDigitYearLength)
        {
            return false;
        }

        var canonical = string.Concat(year, "-", month.PadLeft(TwoDigitYearLength, '0'), "-", day.PadLeft(TwoDigitYearLength, '0'));

        return DateTime.TryParseExact(
            canonical,
            IsoFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out result);
    }

    public static bool TrySplit(string? value, out string first, out string second, out string third)
    {
        first = second = third = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var match = DatePattern.Match(value);
        if (!match.Success)
        {
            return false;
        }

        first = match.Groups["a"].Value;
        second = match.Groups["b"].Value;
        third = match.Groups["c"].Value;
        return true;
    }

    public static int ExpandTwoDigitYear(int twoDigits, int currentYear) =>
        twoDigits <= currentYear % CenturyDivisor
            ? TwentyFirstCentury + twoDigits
            : TwentiethCentury + twoDigits;

    public static string Format(DateTime value) => value.ToString(IsoFormat, CultureInfo.InvariantCulture);
}
