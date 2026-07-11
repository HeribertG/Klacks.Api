// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared name-matching primitives for the by-name resolver helpers: accent- and
/// punctuation-insensitive normalization, tokenization, length-scaled fuzzy comparison and
/// Kölner Phonetik token codes, so user-supplied names still resolve when accents are missing,
/// single characters are damaged (e.g. console-encoding mojibake) or a spoken name was
/// misheard by STT ("Meier" vs "Mayer").
/// </summary>

using System.Globalization;
using System.Text;
using Klacks.Api.Domain.Services.Assistant.Phonetics;

namespace Klacks.Api.Application.Skills;

internal static class NameMatching
{
    private const int ExactOnlyMaxLength = 3;
    private const int SingleEditMaxLength = 7;
    private const int MaxEditDistance = 2;
    private const int PhoneticMinTokenLength = 2;
    private const char TokenSeparator = ' ';

    private static readonly KoelnerPhoneticEncoder PhoneticEncoder = new();

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

    public static string[] PhoneticCodes(string normalized)
    {
        return Tokenize(normalized)
            .Where(token => token.Length >= PhoneticMinTokenLength)
            .Select(PhoneticEncoder.Encode)
            .Where(code => code.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    // Coverage direction mirrors TokensFuzzyCovered: every phonetic code of the stored name
    // must appear among the query's codes, which absorbs label words in the query.
    public static bool PhoneticTokensCovered(string[] nameCodes, string[] queryCodes)
    {
        return nameCodes.Length > 0
               && nameCodes.All(queryCodes.Contains);
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
