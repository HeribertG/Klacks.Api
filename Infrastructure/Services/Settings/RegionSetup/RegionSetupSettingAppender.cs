// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Appends optional numeric and boolean profile values to a planned settings list in their invariant
/// string form; an absent value adds nothing, so the stored setting keeps inheriting its default.
/// </summary>
/// <param name="settings">Planned (type, value) setting writes</param>
/// <param name="type">Settings key to write</param>
/// <param name="value">Optional profile value</param>

using System.Globalization;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

internal static class RegionSetupSettingAppender
{
    private const string TrueValue = "true";
    private const string FalseValue = "false";

    internal static void AddNumber(List<(string Type, string Value)> settings, string type, decimal? value)
    {
        if (value.HasValue)
        {
            settings.Add((type, value.Value.ToString(CultureInfo.InvariantCulture)));
        }
    }

    internal static void AddInt(List<(string Type, string Value)> settings, string type, int? value)
    {
        if (value.HasValue)
        {
            settings.Add((type, value.Value.ToString(CultureInfo.InvariantCulture)));
        }
    }

    internal static void AddBool(List<(string Type, string Value)> settings, string type, bool? value)
    {
        if (value.HasValue)
        {
            settings.Add((type, value.Value ? TrueValue : FalseValue));
        }
    }
}
