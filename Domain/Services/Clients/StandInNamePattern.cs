// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The name pattern of stand-ins created in bulk, e.g. "Fair {n}": the text before and after the index placeholder
/// (StandInConstants.IndexPlaceholder) stays, the placeholder becomes a running index. The next free index is one above
/// the highest index an existing name carries in the same frame, or 1 when none does.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Clients;

public static class StandInNamePattern
{
    /// <summary>True when the pattern holds the index placeholder exactly once.</summary>
    /// <param name="pattern">Name pattern supplied by the caller</param>
    public static bool HasSingleIndexPlaceholder(string? pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        var first = pattern.IndexOf(StandInConstants.IndexPlaceholder, StringComparison.Ordinal);
        return first >= 0
            && pattern.IndexOf(StandInConstants.IndexPlaceholder, first + 1, StringComparison.Ordinal) < 0;
    }

    /// <summary>The text before the index placeholder.</summary>
    /// <param name="pattern">Name pattern with exactly one index placeholder</param>
    public static string Prefix(string pattern) =>
        pattern[..pattern.IndexOf(StandInConstants.IndexPlaceholder, StringComparison.Ordinal)];

    /// <summary>The text after the index placeholder.</summary>
    /// <param name="pattern">Name pattern with exactly one index placeholder</param>
    public static string Suffix(string pattern) =>
        pattern[(pattern.IndexOf(StandInConstants.IndexPlaceholder, StringComparison.Ordinal) + StandInConstants.IndexPlaceholder.Length)..];

    /// <summary>The name for one index.</summary>
    /// <param name="pattern">Name pattern with exactly one index placeholder</param>
    /// <param name="index">Running index that replaces the placeholder</param>
    public static string Expand(string pattern, int index) =>
        Prefix(pattern) + index.ToString(CultureInfo.InvariantCulture) + Suffix(pattern);

    /// <summary>
    /// One above the highest index found in the given names, or 1. A name counts only when the text between prefix and
    /// suffix consists of ASCII digits alone and fits an int; the result is a long, so it can lie above int.MaxValue.
    /// </summary>
    /// <param name="pattern">Name pattern with exactly one index placeholder</param>
    /// <param name="existingNames">Names of existing clients that start with the prefix and end with the suffix</param>
    public static long NextFreeIndex(string pattern, IEnumerable<string> existingNames)
    {
        var prefix = Prefix(pattern);
        var suffix = Suffix(pattern);
        var highest = 0;

        foreach (var name in existingNames)
        {
            if (name.Length <= prefix.Length + suffix.Length
                || !name.StartsWith(prefix, StringComparison.Ordinal)
                || !name.EndsWith(suffix, StringComparison.Ordinal))
            {
                continue;
            }

            var digits = name[prefix.Length..^suffix.Length];
            if (digits.All(char.IsAsciiDigit)
                && int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var index)
                && index > highest)
            {
                highest = index;
            }
        }

        return (long)highest + 1;
    }
}
