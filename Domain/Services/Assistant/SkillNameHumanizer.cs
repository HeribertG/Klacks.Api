// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Turns a raw skill name into the label a person reads in the correction menu. Skills are database-
/// and plugin-driven, so no static translation key set can cover them; the label is derived from the
/// name itself instead - separators become spaces, runs of them collapse, and the first letter is
/// capitalised.
/// </summary>
/// <param name="skillName">Raw skill name as it appears in the toolset, e.g. get_page_controls</param>

using System.Globalization;
using System.Text;

namespace Klacks.Api.Domain.Services.Assistant;

public static class SkillNameHumanizer
{
    private const char Space = ' ';
    private const char Underscore = '_';
    private const char Dash = '-';

    public static string ToDisplayName(string? skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(skillName.Length);
        var pendingSpace = false;

        foreach (var character in skillName)
        {
            if (character == Underscore || character == Dash || char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(Space);
                pendingSpace = false;
            }

            builder.Append(character);
        }

        if (builder.Length == 0)
        {
            return string.Empty;
        }

        builder[0] = char.ToUpper(builder[0], CultureInfo.InvariantCulture);
        return builder.ToString();
    }
}
