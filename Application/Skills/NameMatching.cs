// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared name-matching primitives for the by-name resolver helpers: accent- and
/// punctuation-insensitive normalization, tokenization and length-scaled fuzzy comparison,
/// so user-supplied names still resolve when accents are missing or single characters are
/// damaged (e.g. console-encoding mojibake).
/// </summary>

using System.Globalization;
using System.Text;

namespace Klacks.Api.Application.Skills;

internal static class NameMatching
{
    private const int ExactOnlyMaxLength = 3;
    private const int SingleEditMaxLength = 7;
    private const int MaxEditDistance = 2;
    private const char TokenSeparator = ' ';

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
            }
            else if (char.IsWhiteSpace(ch) && builder.Length > 0 && builder[^1] != TokenSeparator)
            {
                builder.Append(TokenSeparator);
            }
        }

        return builder.ToString().TrimEnd(TokenSeparator);
    }

    public static string[] Tokenize(string normalized)
    {
        return normalized.Split(TokenSeparator, StringSplitOptions.RemoveEmptyEntries);
    }

    public static bool FuzzyEquals(string left, string right)
    {
        if (left == right)
        {
            return true;
        }

        var tolerance = ToleranceFor(Math.Max(left.Length, right.Length));
        if (tolerance == 0 || Math.Abs(left.Length - right.Length) > tolerance)
        {
            return false;
        }

        return Levenshtein(left, right) <= tolerance;
    }

    public static bool TokensFuzzyCovered(string[] nameTokens, string[] queryTokens)
    {
        return nameTokens.Length > 0
               && nameTokens.All(nameToken => queryTokens.Any(queryToken => FuzzyEquals(nameToken, queryToken)));
    }

    private static int ToleranceFor(int length)
    {
        if (length <= ExactOnlyMaxLength)
        {
            return 0;
        }

        return length <= SingleEditMaxLength ? 1 : MaxEditDistance;
    }

    private static int Levenshtein(string left, string right)
    {
        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];

        for (var j = 0; j <= right.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= left.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= right.Length; j++)
            {
                var substitutionCost = left[i - 1] == right[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + substitutionCost);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }
}
