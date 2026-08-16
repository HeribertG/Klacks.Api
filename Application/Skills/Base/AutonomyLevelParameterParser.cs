// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Parses a skill's "level" parameter into an AutonomyLevel, accepting either the numeric value
/// (0-3) or the enum name. Shared by every skill that takes an autonomy level as input.
/// </summary>
/// <param name="value">Raw parameter value (numeric string or enum name)</param>
/// <param name="level">The parsed level; AutonomyDefaults.DefaultLevel when parsing fails</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Skills.Base;

public static class AutonomyLevelParameterParser
{
    public static bool TryParse(string? value, out AutonomyLevel level)
    {
        level = AutonomyDefaults.DefaultLevel;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (int.TryParse(value, out var numeric))
        {
            if (numeric < (int)AutonomyDefaults.MinimumLevel || numeric > (int)AutonomyDefaults.MaximumLevel)
            {
                return false;
            }

            level = (AutonomyLevel)numeric;
            return true;
        }

        return Enum.TryParse(value, ignoreCase: true, out level)
            && Enum.IsDefined(level);
    }
}
