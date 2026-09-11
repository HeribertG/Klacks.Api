// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared parse logic for DateOnlyJsonConverter and DateOnlyNullableJsonConverter, so both read a
/// wire date identically: a bare calendar date (yyyy-MM-dd) is read directly; anything else is
/// delegated to UtcDateTimeReader — the same grammar UtcDateTimeJsonConverter uses for DateTime — so
/// a DateOnly and a DateTime property never disagree about which strings are valid or which calendar
/// day a value with a UTC offset resolves to. A non-zero offset is rejected instead of silently
/// shifting the calendar day, and a value without any offset is read as UTC, so the result no longer
/// depends on the server process's time zone.
/// </summary>

using System.Globalization;
using System.Text.Json;

namespace Klacks.Api.Infrastructure.Converters;

internal static class DateOnlyStringParser
{
    public const string InvalidFormatMessage = "The value is not a valid ISO 8601 date.";

    public static DateOnly Parse(ref Utf8JsonReader reader, string s)
    {
        if (DateOnly.TryParseExact(
                s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
        {
            return dateOnly;
        }

        var utc = UtcDateTimeReader.ReadUtc(ref reader);
        return DateOnly.FromDateTime(utc);
    }
}
