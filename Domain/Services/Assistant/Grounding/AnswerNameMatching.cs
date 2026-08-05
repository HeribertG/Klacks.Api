// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Claim-side name matching for the grounding evaluator: accent-insensitive normalization and a
/// deliberately strict comparison (exact, or Levenshtein distance 1 when both tokens have at
/// least four letters). No phonetic path — phonetics ground unrelated words and the claim side
/// must prefer silence over a false finding.
/// </summary>

using System.Globalization;
using System.Text;

namespace Klacks.Api.Domain.Services.Assistant.Grounding;

public static class AnswerNameMatching
{
    private const int MinFuzzyTokenLength = 4;
    private const int MaxEditDistance = 1;
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

            if (char.IsLetter(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
            }
            else if (builder.Length > 0 && builder[^1] != TokenSeparator)
            {
                builder.Append(TokenSeparator);
            }
        }

        return builder.ToString().TrimEnd(TokenSeparator);
    }

    public static string[] Tokenize(string? value)
        => Normalize(value).Split(TokenSeparator, StringSplitOptions.RemoveEmptyEntries);

    public static bool StrictEquals(string left, string right)
    {
        if (left.Length == 0 || right.Length == 0)
        {
            return false;
        }

        if (string.Equals(left, right, StringComparison.Ordinal))
        {
            return true;
        }

        if (left.Length < MinFuzzyTokenLength || right.Length < MinFuzzyTokenLength)
        {
            return false;
        }

        if (Math.Abs(left.Length - right.Length) > MaxEditDistance)
        {
            return false;
        }

        return Levenshtein(left, right) <= MaxEditDistance;
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
