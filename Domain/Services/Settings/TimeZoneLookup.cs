// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single tolerant time-zone lookup shared by every caller that needs to turn a caller-supplied id (a
/// setting value, an LLM skill parameter, a scheduling cron time zone) into a <see cref="TimeZoneInfo"/>
/// without throwing on an unknown or malformed id.
/// </summary>

using System.Diagnostics.CodeAnalysis;

namespace Klacks.Api.Domain.Services.Settings;

public static class TimeZoneLookup
{
    public static bool TryResolve(string? timeZoneId, [NotNullWhen(true)] out TimeZoneInfo? zone)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            zone = null;
            return false;
        }

        return TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId.Trim(), out zone);
    }
}
