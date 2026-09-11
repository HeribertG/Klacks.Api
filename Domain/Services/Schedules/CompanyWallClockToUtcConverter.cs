// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Converts a company-local wall-clock value (Kind=Unspecified, e.g. built from a DateOnly + TimeOnly)
/// to its correct UTC instant, DST-safe. A plain TimeZoneInfo.ConvertTimeToUtc silently mishandles the
/// two DST transition edge cases: a spring-forward gap (the wall clock never showed that minute) throws,
/// and a fall-back ambiguity (the wall clock shows that minute twice) resolves to .NET's default guess
/// instead of a documented choice. This helper makes both cases explicit: a gap value is advanced to the
/// next valid minute, an ambiguous value resolves to its earlier (daylight-saving) occurrence.
/// </summary>

namespace Klacks.Api.Domain.Services.Schedules;

public static class CompanyWallClockToUtcConverter
{
    private const int MaxGapSearchMinutes = 180;

    /// <summary>
    /// Converts a company-local wall-clock <see cref="DateTime"/> (Kind must be Unspecified) to UTC.
    /// </summary>
    /// <param name="wallClock">The local wall-clock value to convert; must have Kind=Unspecified.</param>
    /// <param name="companyTimeZone">The company's configured time zone (from ICompanyClock.GetTimeZoneAsync).</param>
    public static DateTime ConvertToUtc(DateTime wallClock, TimeZoneInfo companyTimeZone)
    {
        if (wallClock.Kind != DateTimeKind.Unspecified)
        {
            throw new ArgumentException(
                $"Expected a wall-clock value with Kind=Unspecified but got {wallClock.Kind}.",
                nameof(wallClock));
        }

        var resolved = companyTimeZone.IsInvalidTime(wallClock)
            ? AdvanceToNextValidTime(wallClock, companyTimeZone)
            : wallClock;

        if (companyTimeZone.IsAmbiguousTime(resolved))
        {
            var earliestOffset = companyTimeZone.GetAmbiguousTimeOffsets(resolved).Max();
            return DateTime.SpecifyKind(resolved - earliestOffset, DateTimeKind.Utc);
        }

        return TimeZoneInfo.ConvertTimeToUtc(resolved, companyTimeZone);
    }

    private static DateTime AdvanceToNextValidTime(DateTime invalidLocal, TimeZoneInfo companyTimeZone)
    {
        var candidate = invalidLocal;
        for (var i = 0; i < MaxGapSearchMinutes; i++)
        {
            candidate = candidate.AddMinutes(1);
            if (!companyTimeZone.IsInvalidTime(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            $"Could not resolve a valid local time within {MaxGapSearchMinutes} minutes after the " +
            $"DST gap starting at {invalidLocal:O} in zone '{companyTimeZone.Id}'.");
    }
}
