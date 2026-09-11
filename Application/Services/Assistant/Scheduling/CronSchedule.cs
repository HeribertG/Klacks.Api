// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Thin, DST-correct wrapper over the Cronos cron engine used by the scheduled-task feature. Parses
/// and validates standard 5-field cron expressions, resolves the next occurrence in a given IANA time
/// zone and renders a human-readable local time for confirmation messages.
/// </summary>

using Cronos;
using Klacks.Api.Domain.Services.Settings;

namespace Klacks.Api.Application.Services.Assistant.Scheduling;

public static class CronSchedule
{
    /// <summary>Returns true when the expression is a valid standard 5-field cron expression.</summary>
    public static bool IsValidExpression(string? expression)
    {
        return TryParse(expression, out _);
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

        if (!TimeZoneLookup.TryResolve(timeZoneId, out var zone))
        {
            return null;
        }

        var fromUtcKind = DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc);
        return cron.GetNextOccurrence(fromUtcKind, zone, inclusive: false);
    }

    /// <summary>Renders a UTC instant as the owner's local wall-clock time for confirmation text.</summary>
    public static string FormatLocal(DateTime utc, string? timeZoneId)
    {
        if (!TimeZoneLookup.TryResolve(timeZoneId, out var zone))
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

}
