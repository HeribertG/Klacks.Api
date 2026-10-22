// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared parse and validation helpers for region-setup profile fields. Every failure is an
/// InvalidRequestException naming the offending profile path, so the whole import fails fast before the
/// first write.
/// </summary>
/// <param name="value">Raw value from the profile file</param>
/// <param name="fieldName">Dotted profile path of the value, used in the error message</param>

using System.Globalization;
using Klacks.Api.Application.Services.Setup;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Services.Settings;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

internal static class RegionSetupFieldParser
{
    private const string RateModeMultiplier = CompanyRuleParameterValues.RateModeMultiplier;
    private const string RateModeFixedPerHour = CompanyRuleParameterValues.RateModeFixedPerHour;
    private const string RateModeFixedPerShift = CompanyRuleParameterValues.RateModeFixedPerShift;

    private const string StackingModeHighestWins = SurchargeStackingModeValues.HighestWins;
    private const string StackingModeAdditive = SurchargeStackingModeValues.Additive;
    private const string OvertimeBasisDay = CompanyRuleParameterValues.OvertimeBasisDay;
    private const string OvertimeBasisWeek = CompanyRuleParameterValues.OvertimeBasisWeek;

    // Lowercases and collapses whitespace runs to single dashes. Used for the industry key and for the
    // name part of import source keys - which makes the NAME the identity: renaming a preset in the
    // file creates a new row and leaves the old one behind (documented on the preset DTO).
    private const int MaxImportSlugLength = 120;

    internal static DayOfWeek ParseDayOfWeek(string value, string fieldName)
    {
        if (!Enum.TryParse<DayOfWeek>(value, ignoreCase: true, out var day))
        {
            throw new InvalidRequestException($"Region setup: '{value}' in {fieldName} is not a valid day of week.");
        }

        return day;
    }

    internal static void ValidateTimeZone(string timeZone)
    {
        if (!TimeZoneLookup.TryResolve(timeZone, out _))
        {
            throw new InvalidRequestException($"Region setup: invalid time zone '{timeZone}'.");
        }
    }

    internal static void ValidateTimeOfDay(string value, string fieldName)
    {
        if (!TimeOnly.TryParseExact(value.Trim(), DateTimeFormats.HourMinute, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            throw new InvalidRequestException($"Region setup: '{value}' in {fieldName} is not a valid HH:mm time.");
        }
    }

    internal static string ValidateStackingMode(string value, string fieldName)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized != StackingModeHighestWins && normalized != StackingModeAdditive)
        {
            throw new InvalidRequestException($"Region setup: '{value}' in {fieldName} must be 'highestWins' or 'additive'.");
        }

        return normalized;
    }

    internal static string ValidateOvertimeBasis(string value, string fieldName)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized != OvertimeBasisDay && normalized != OvertimeBasisWeek)
        {
            throw new InvalidRequestException($"Region setup: '{value}' in {fieldName} must be 'day' or 'week'.");
        }

        return normalized;
    }

    internal static string ValidateOvertimeRateMode(string value, string fieldName)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized == RateModeFixedPerShift)
        {
            throw new InvalidRequestException(
                $"Region setup: '{value}' in {fieldName} is not supported — a flat per-shift amount cannot be split across overtime tiers by hours worked. Use 'multiplier' or 'fixedPerHour'.");
        }

        if (normalized != RateModeMultiplier && normalized != RateModeFixedPerHour)
        {
            throw new InvalidRequestException($"Region setup: '{value}' in {fieldName} must be 'multiplier' or 'fixedPerHour'.");
        }

        return normalized;
    }

    internal static void RequireNonNegative(decimal? value, string fieldName)
    {
        if (value < 0)
        {
            throw new InvalidRequestException($"Region setup: {fieldName} must not be negative.");
        }
    }

    internal static void RequireNonNegative(int? value, string fieldName)
    {
        if (value < 0)
        {
            throw new InvalidRequestException($"Region setup: {fieldName} must not be negative.");
        }
    }

    internal static string BuildImportSlug(string value)
    {
        var trimmed = value.Trim().ToLowerInvariant();
        var slug = string.Join('-', trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (slug.Length <= MaxImportSlugLength)
        {
            return slug;
        }

        // Keep the persisted import source key within its column length (varchar(200)) even for very
        // long free-text names: truncate the slug and append a stable content hash so uniqueness holds.
        var hashSuffix = ImportContentHasher.ComputeHash(value)[..8].ToLowerInvariant();
        return string.Concat(slug.AsSpan(0, MaxImportSlugLength - hashSuffix.Length - 1), "-", hashSuffix);
    }
}
