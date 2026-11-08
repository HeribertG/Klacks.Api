// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Key-specific setting value validation for the generic settings key/value store. SettingKeys.
/// ActiveIndustries accepts empty, the IndustrySlugs.Custom marker, a single known
/// industry slug, or a legacy comma-separated list of known slugs (case-insensitive, trimmed);
/// SettingKeys.KlacksyProactiveAutonomyLevel accepts an AutonomyLevel integer (0-3);
/// SettingKeys.DatevConsultantNumber and SettingKeys.DatevClientNumber accept empty or a whole number inside
/// the DatevNumberRanges (consultant 1001-9999999, client 1-99999); the scheduling caps SettingKeys.SchedulingMaxDailyHours,
/// SettingKeys.SchedulingMaxWeeklyHours and SettingKeys.SchedulingMaxConsecutiveDays accept empty or a number of at
/// least SchedulingCapLimits.MinValue (a cap is never 0). New key-specific
/// rules are added by extending the dispatch in Validate, not by touching the settings handlers.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Domain.Services.Settings;

public class SettingValueValidator : ISettingValueValidator
{
    private const char SlugSeparator = ',';

    private static readonly string[] SchedulingCapKeys =
    [
        SettingKeys.SchedulingMaxDailyHours,
        SettingKeys.SchedulingMaxWeeklyHours,
        SettingKeys.SchedulingMaxConsecutiveDays
    ];

    public void Validate(string key, string value)
    {
        if (string.Equals(key, SettingKeys.ActiveIndustries, StringComparison.Ordinal))
        {
            ValidateActiveIndustries(value ?? string.Empty);
        }

        if (string.Equals(key, SettingKeys.KlacksyProactiveAutonomyLevel, StringComparison.Ordinal))
        {
            ValidateAutonomyLevel(value ?? string.Empty);
        }

        if (string.Equals(key, SettingKeys.DatevConsultantNumber, StringComparison.Ordinal))
        {
            ValidateOptionalWholeNumber(
                SettingKeys.DatevConsultantNumber,
                value,
                DatevNumberRanges.ConsultantNumberMin,
                DatevNumberRanges.ConsultantNumberMax);
        }

        if (string.Equals(key, SettingKeys.DatevClientNumber, StringComparison.Ordinal))
        {
            ValidateOptionalWholeNumber(
                SettingKeys.DatevClientNumber,
                value,
                DatevNumberRanges.ClientNumberMin,
                DatevNumberRanges.ClientNumberMax);
        }

        if (SchedulingCapKeys.Contains(key, StringComparer.Ordinal))
        {
            ValidateSchedulingCap(key, value);
        }
    }

    private static void ValidateSchedulingCap(string key, string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return;
        }

        if (!decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
            || !SchedulingCapLimits.IsValid(number))
        {
            throw new InvalidRequestException(
                $"Setting '{key}' must be empty or a number of at least {SchedulingCapLimits.MinValue}, got '{value}'.");
        }
    }

    private static void ValidateOptionalWholeNumber(string key, string? value, int min, int max)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return;
        }

        if (!int.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            || number < min
            || number > max)
        {
            throw new InvalidRequestException(
                $"Setting '{key}' must be empty or a whole number between {min} and {max}, got '{value}'.");
        }
    }

    private static void ValidateAutonomyLevel(string value)
    {
        if (!int.TryParse(value, out var level)
            || !Enum.IsDefined(typeof(AutonomyLevel), level))
        {
            throw new InvalidRequestException(
                $"Setting '{SettingKeys.KlacksyProactiveAutonomyLevel}' must be an autonomy level " +
                $"between {(int)AutonomyLevel.Propose} and {(int)AutonomyLevel.FullyAutonomous}, got '{value}'.");
        }
    }

    private static void ValidateActiveIndustries(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return;
        }

        var slugs = trimmed
            .Split(SlugSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (slugs.Count == 0)
        {
            return;
        }

        var hasCustom = slugs.Any(slug => string.Equals(slug, IndustrySlugs.Custom, StringComparison.OrdinalIgnoreCase));
        if (hasCustom)
        {
            if (slugs.Count > 1)
            {
                throw new InvalidRequestException(
                    $"Setting '{SettingKeys.ActiveIndustries}' must not combine '{IndustrySlugs.Custom}' with other industry slugs.");
            }

            return;
        }

        foreach (var slug in slugs)
        {
            if (!IndustrySlugs.All.Any(known => string.Equals(known, slug, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidRequestException(
                    $"Setting '{SettingKeys.ActiveIndustries}' contains unknown industry slug '{slug}'.");
            }
        }
    }
}
