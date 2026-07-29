// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Formats DateTime values as PostgreSQL literals that carry an explicit UTC offset. Seed data is
/// emitted as raw SQL, so a literal without an offset would be resolved against the database
/// session time zone and silently shift the stored instant on a non-UTC server.
/// </summary>

using System.Globalization;

namespace Klacks.Api.Data.Seed;

public static class SeedSqlTimestamp
{
    private const string UtcTimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff'+00'";

    private const string UtcMidnightFormat = "yyyy-MM-dd '00:00:00+00'";

    /// <summary>
    /// Renders the instant of a value as a timestamptz literal in UTC.
    /// </summary>
    /// <param name="value">Point in time; Local is converted, Unspecified is taken as UTC</param>
    public static string ToLiteral(DateTime value) =>
        ToUtc(value).ToString(UtcTimestampFormat, CultureInfo.InvariantCulture);

    /// <summary>
    /// Renders the calendar day of a value as UTC midnight, the project convention for calendar
    /// markers such as ValidFrom stored in timestamptz columns.
    /// </summary>
    /// <param name="value">Calendar day; the time component is discarded</param>
    public static string ToUtcMidnightLiteral(DateTime value) =>
        value.ToString(UtcMidnightFormat, CultureInfo.InvariantCulture);

    /// <summary>
    /// Renders a calendar day as UTC midnight.
    /// </summary>
    /// <param name="value">Calendar day to render</param>
    public static string ToUtcMidnightLiteral(DateOnly value) =>
        value.ToString(UtcMidnightFormat, CultureInfo.InvariantCulture);

    /// <summary>
    /// Renders an optional instant; an absent value yields an empty string, which keeps the
    /// behaviour of the previous inline format specifiers.
    /// </summary>
    /// <param name="value">Optional point in time</param>
    public static string ToLiteral(DateTime? value) =>
        value.HasValue ? ToLiteral(value.Value) : string.Empty;

    /// <summary>
    /// Renders an optional calendar day as UTC midnight; an absent value yields an empty string.
    /// </summary>
    /// <param name="value">Optional calendar day</param>
    public static string ToUtcMidnightLiteral(DateTime? value) =>
        value.HasValue ? ToUtcMidnightLiteral(value.Value) : string.Empty;

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
