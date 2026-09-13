// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The one deterministic policy for turning a calendar string an LLM handed to a skill into a
/// DateTime, DateOnly or TimeOnly. ISO 8601 is tried first and always with InvariantCulture, so an ISO
/// value can never be re-interpreted by a calendar (th-TH would read "2026-03-04" as Gregorian 1483,
/// ar-SA would reject it). Only if the value is not ISO are the ambiguous culture formats tried, and
/// the cultures come from the user's UI language rather than the server's process culture - "03/04/2026"
/// is 4 March for an English user and 3 April for a French one. Everything is routed through one
/// DateTimeOffset parse and derived from it, so the DateOnly and the DateTime path can never disagree
/// about the day. The written calendar day and wall clock are kept exactly as given and the result is
/// forced to Kind=Utc: callers store it in a 'timestamp with time zone' column as a calendar boundary,
/// and converting an offset to its UTC instant would silently move "2026-08-01T00:00:00+02:00" to the
/// previous day. A parsed year outside the plausible band is rejected rather than stored as written.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant.Skills;

public static class SkillCalendarStringParser
{
    private const DateTimeStyles CalendarParseStyles =
        DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces;

    /// <param name="raw">Value as the LLM wrote it</param>
    /// <param name="language">UI language of the user the value came from; null keeps the default cultures</param>
    /// <param name="value">Parsed value with Kind=Utc and the written day and wall clock unchanged</param>
    public static bool TryParseDateTime(string? raw, string? language, out DateTime value)
    {
        if (TryParseOffset(raw, language, SkillDateParsingDefaults.IsoDateTimeFormats, out var parsed))
        {
            value = DateTime.SpecifyKind(parsed.DateTime, DateTimeKind.Utc);
            return true;
        }

        value = default;
        return false;
    }

    /// <param name="raw">Value as the LLM wrote it</param>
    /// <param name="language">UI language of the user the value came from; null keeps the default cultures</param>
    /// <param name="value">Parsed calendar day, identical to the day of <see cref="TryParseDateTime"/></param>
    public static bool TryParseDateOnly(string? raw, string? language, out DateOnly value)
    {
        if (TryParseDateTime(raw, language, out var parsed))
        {
            value = DateOnly.FromDateTime(parsed);
            return true;
        }

        value = default;
        return false;
    }

    /// <param name="raw">Value as the LLM wrote it</param>
    /// <param name="language">UI language of the user the value came from; null keeps the default cultures</param>
    /// <param name="value">Parsed wall-clock time</param>
    public static bool TryParseTimeOnly(string? raw, string? language, out TimeOnly value)
    {
        if (TryParseOffset(raw, language, SkillDateParsingDefaults.IsoTimeFormats, out var parsed))
        {
            value = TimeOnly.FromDateTime(parsed.DateTime);
            return true;
        }

        value = default;
        return false;
    }

    private static bool TryParseOffset(
        string? raw, string? language, string[] isoFormats, out DateTimeOffset value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var trimmed = raw.Trim();
        if (DateTimeOffset.TryParseExact(
                trimmed, isoFormats, CultureInfo.InvariantCulture, CalendarParseStyles, out value))
        {
            return true;
        }

        foreach (var culture in SkillDateCultureResolver.CulturesFor(language))
        {
            if (DateTimeOffset.TryParse(trimmed, culture, CalendarParseStyles, out var parsed) &&
                SkillDateParsingDefaults.IsPlausibleCalendarYear(parsed.Year))
            {
                value = parsed;
                return true;
            }
        }

        return false;
    }
}
