// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Parses the time budget (fromTime, untilTime) and the validity period (fromDate, untilDate) the skills that build
/// containers take as text, in the language of the user. Each parser returns the parsed values or the refusal text the
/// skill answers with; the budget must end after it starts and the validity must not end before it starts.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Application.Services.Schedules;

public static class ContainerSkillInputParser
{
    /// <param name="fromTimeStr">Start of the day's time budget as the user said it (HH:mm)</param>
    /// <param name="untilTimeStr">End of the day's time budget as the user said it (HH:mm)</param>
    /// <param name="language">Language of the user, used to read the clock times</param>
    public static (TimeOnly FromTime, TimeOnly UntilTime, string? Error) ParseTimeBudget(
        string fromTimeStr, string untilTimeStr, string? language)
    {
        if (!SkillCalendarStringParser.TryParseTimeOnly(fromTimeStr, language, out var fromTime))
        {
            return (default, default, $"Invalid fromTime format: {fromTimeStr}. Expected HH:mm");
        }

        if (!SkillCalendarStringParser.TryParseTimeOnly(untilTimeStr, language, out var untilTime))
        {
            return (default, default, $"Invalid untilTime format: {untilTimeStr}. Expected HH:mm");
        }

        if (untilTime.ToTimeSpan() <= fromTime.ToTimeSpan())
        {
            return (default, default, $"untilTime ({untilTimeStr}) must be after fromTime ({fromTimeStr}).");
        }

        return (fromTime, untilTime, null);
    }

    /// <param name="fromDateStr">First day of validity as the user said it (YYYY-MM-DD)</param>
    /// <param name="untilDateStr">Optional last day of validity; null or blank = open-ended</param>
    /// <param name="language">Language of the user, used to read the dates</param>
    public static (DateOnly FromDate, DateOnly? UntilDate, string? Error) ParseValidity(
        string fromDateStr, string? untilDateStr, string? language)
    {
        if (!SkillCalendarStringParser.TryParseDateOnly(fromDateStr, language, out var fromDate))
        {
            return (default, null, $"Invalid fromDate format: {fromDateStr}. Expected YYYY-MM-DD (e.g. 2026-06-01).");
        }

        DateOnly? untilDate = null;
        if (!string.IsNullOrWhiteSpace(untilDateStr))
        {
            if (!SkillCalendarStringParser.TryParseDateOnly(untilDateStr, language, out var parsedUntilDate))
            {
                return (default, null, $"Invalid untilDate format: {untilDateStr}. Expected YYYY-MM-DD.");
            }

            untilDate = parsedUntilDate;
            if (untilDate.Value < fromDate)
            {
                return (default, null,
                    $"untilDate ({untilDate.Value.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)}) must not be before fromDate ({fromDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)}).");
            }
        }

        return (fromDate, untilDate, null);
    }
}
