// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Makes a name that comes from stored data safe to quote into a skill message the model reads: control characters
/// and line breaks become single spaces, runs of whitespace collapse, and the length is capped. The full name stays
/// in the structured result data; only the prose message is shortened.
/// </summary>

using System.Text;

namespace Klacks.Api.Domain.Services.Assistant;

public static class SkillMessageText
{
    public const int MaxNameLength = 80;
    private const char Ellipsis = '…';

    /// <summary>
    /// Returns the message-safe form of a data name; null stays null.
    /// </summary>
    /// <param name="value">Name as stored, e.g. a holiday or contract name</param>
    public static string? Name(string? value)
    {
        if (value == null)
        {
            return null;
        }

        var builder = new StringBuilder(value.Length);
        var lastWasSpace = false;
        foreach (var character in value)
        {
            var isSpace = char.IsControl(character) || char.IsWhiteSpace(character);
            if (isSpace)
            {
                if (!lastWasSpace && builder.Length > 0)
                {
                    builder.Append(' ');
                }

                lastWasSpace = true;
                continue;
            }

            builder.Append(character);
            lastWasSpace = false;
        }

        var cleaned = builder.ToString().TrimEnd();
        return cleaned.Length <= MaxNameLength ? cleaned : cleaned[..MaxNameLength] + Ellipsis;
    }
}
