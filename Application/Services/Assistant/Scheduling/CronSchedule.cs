// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Thin, DST-correct wrapper over the Cronos cron engine used by the scheduled-task feature. Parses
/// and validates standard 5-field cron expressions, resolves the next occurrence in a given IANA time
/// zone and renders a human-readable local time for confirmation messages.
/// </summary>

using Cronos;

namespace Klacks.Api.Application.Services.Assistant.Scheduling;

public static class CronSchedule
{
    /// <summary>Returns true when the expression is a valid standard 5-field cron expression.</summary>
    public static bool IsValidExpression(string? expression)
    {
        return TryParse(expression, out _);
    }

    /// <summary>Returns true when the id resolves to a system time zone.</summary>
    public static bool IsValidTimeZone(string? timeZoneId)
    {
        return TryGetTimeZone(timeZoneId, out _);
    }

    /// <summary>
    /// Computes the next run strictly after <paramref name="fromUtc"/> in the given time zone, or null
    /// when the expression/zone is invalid or has no upcoming occurrence.
    /// </summary>
    public static DateTime? GetNextOccurrenceUtc(string? expression, string? timeZoneId, DateTime fromUtc)
    {
        if (!TryParse(expression, out var cron) || cron is null)
        {
            return null;
        }

        if (!TryGetTimeZone(timeZoneId, out var zone) || zone is null)
        {
            return null;
        }

        var fromUtcKind = DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc);
        return cron.GetNextOccurrence(fromUtcKind, zone, inclusive: false);
    }

    /// <summary>Renders a UTC instant as the owner's local wall-clock time for confirmation text.</summary>
    public static string FormatLocal(DateTime utc, string? timeZoneId)
    {
        if (!TryGetTimeZone(timeZoneId, out var zone) || zone is null)
        {
            return $"{DateTime.SpecifyKind(utc, DateTimeKind.Utc):yyyy-MM-dd HH:mm} UTC";
        }

        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone);
        return $"{local:ddd yyyy-MM-dd HH:mm} {timeZoneId}";
    }

    private static bool TryParse(string? expression, out CronExpression? cron)
    {
        cron = null;
        if (string.IsNullOrWhiteSpace(expression))
        {
            return false;
        }

        try
        {
            cron = CronExpression.Parse(expression.Trim(), CronFormat.Standard);
            return true;
        }
        catch (CronFormatException)
        {
            return false;
        }
    }

    private static bool TryGetTimeZone(string? timeZoneId, out TimeZoneInfo? zone)
    {
        zone = null;
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
            return true;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return false;
        }
    }
}
