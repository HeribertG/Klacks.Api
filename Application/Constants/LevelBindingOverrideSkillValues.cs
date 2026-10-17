// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill parameter values of levelBindingOverride (set_shift_required_qualification) and their mapping onto
/// ShiftRequiredQualification.LevelBindingOverride: inherit = null (as the qualification), binding = true,
/// hint = false.
/// </summary>

namespace Klacks.Api.Application.Constants;

public static class LevelBindingOverrideSkillValues
{
    public const string Inherit = "inherit";
    public const string Binding = "binding";
    public const string Hint = "hint";

    public static readonly IReadOnlyList<string> All = [Inherit, Binding, Hint];

    /// <summary>Parses a skill value case-insensitively; false when the value is not one of <see cref="All"/>.</summary>
    public static bool TryParse(string value, out bool? levelBindingOverride)
    {
        var normalized = value.Trim();
        if (string.Equals(normalized, Binding, StringComparison.OrdinalIgnoreCase))
        {
            levelBindingOverride = true;
            return true;
        }

        if (string.Equals(normalized, Hint, StringComparison.OrdinalIgnoreCase))
        {
            levelBindingOverride = false;
            return true;
        }

        levelBindingOverride = null;
        return string.Equals(normalized, Inherit, StringComparison.OrdinalIgnoreCase);
    }

    public static string Describe(bool? levelBindingOverride) => levelBindingOverride switch
    {
        true => Binding,
        false => Hint,
        null => Inherit,
    };
}
