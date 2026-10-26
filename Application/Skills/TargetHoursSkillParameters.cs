// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Parameter names of the target-hours skills and the parser of their optional month (yyyy-MM, or any date in the
/// month in the user's culture; default: the company's current month).
/// </summary>
/// <param name="parameters">Raw skill parameters as the model supplied them</param>
/// <param name="userLanguage">Culture a plain date is read in</param>
/// <param name="companyClock">Company "today" for the default month</param>

using System.Globalization;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Application.Skills;

internal static class TargetHoursSkillParameters
{
    public const string Month = "month";
    public const string DesiredWorkloadPercent = "desiredWorkloadPercent";

    private const string YearMonthFormat = "yyyy-MM";

    public static async Task<(bool Ok, DateOnly DayInPeriod, string? Error)> ReadMonthAsync(
        Dictionary<string, object> parameters, string? userLanguage, ICompanyClock companyClock, CancellationToken cancellationToken)
    {
        var raw = SkillParameterReader.Read<string>(parameters, Month, null);
        if (string.IsNullOrWhiteSpace(raw))
        {
            var today = await companyClock.GetTodayDateAsync(cancellationToken);
            return (true, new DateOnly(today.Year, today.Month, 1), null);
        }

        if (DateOnly.TryParseExact(raw.Trim(), YearMonthFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var yearMonth))
        {
            return (true, yearMonth, null);
        }

        if (SkillCalendarStringParser.TryParseDateOnly(raw, userLanguage, out var date))
        {
            return (true, date, null);
        }

        return (false, default, $"Invalid month: {raw}. Expected yyyy-MM or a date yyyy-MM-dd.");
    }
}
