// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Key-specific setting value validation for the generic settings key/value store. Currently only
/// SettingKeys.ActiveIndustries has a rule: empty, the IndustrySlugs.Custom marker, a single known
/// industry slug, or a legacy comma-separated list of known slugs (case-insensitive, trimmed) are
/// accepted, everything else is rejected. New key-specific rules are added by extending the
/// dispatch in Validate, not by touching the settings handlers.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Domain.Services.Settings;

public class SettingValueValidator : ISettingValueValidator
{
    private const char SlugSeparator = ',';

    public void Validate(string key, string value)
    {
        if (string.Equals(key, SettingKeys.ActiveIndustries, StringComparison.Ordinal))
        {
            ValidateActiveIndustries(value ?? string.Empty);
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
