// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Splits text into name words and builds the folded phrase keys both the dictionary and the matcher use, so a
/// stored name and a name in the text always produce the same key. Words are Unicode letters, joined inside by an
/// apostrophe or hyphen (D'Angelo, Müller-Lüdenscheidt); an English possessive 's stays outside the word. Two words
/// form a phrase only when whitespace or a comma separates them, never across a full stop. Folding ignores case and
/// accents and has two forms for umlauts: ü as ue and ü as u.
/// </summary>

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

public static partial class PseudonymWords
{
    public const int FoldModeCount = 2;

    private const char SpaceGap = ' ';
    private const char CommaGap = ',';

    public static IReadOnlyList<Match> Split(string text) => WordPattern().Matches(text);

    /// <summary>
    /// The gap marker between two words of a phrase: a space for whitespace, a comma for a comma with optional
    /// whitespace, null when the words cannot belong to one name.
    /// </summary>
    public static char? GapMarker(string text, int start, int end)
    {
        if (end <= start)
        {
            return null;
        }

        var commaSeen = false;
        for (var i = start; i < end; i++)
        {
            var c = text[i];
            if (c == CommaGap && !commaSeen)
            {
                commaSeen = true;
                continue;
            }

            if (!char.IsWhiteSpace(c))
            {
                return null;
            }
        }

        return commaSeen ? CommaGap : SpaceGap;
    }

    /// <summary>
    /// Folded keys of a whole surface string (one per fold mode), or an empty list when the surface is not a
    /// phrase of at most the given number of words or has too few letters.
    /// </summary>
    public static IReadOnlyList<string> PhraseKeys(string? surface, int maxWords, int minimumLetters)
    {
        if (string.IsNullOrWhiteSpace(surface))
        {
            return [];
        }

        var trimmed = surface.Trim();
        var words = Split(trimmed);
        if (words.Count == 0 || words.Count > maxWords || words[0].Index != 0
            || words[^1].Index + words[^1].Length != trimmed.Length)
        {
            return [];
        }

        var keys = new List<string>(FoldModeCount);
        for (var mode = 0; mode < FoldModeCount; mode++)
        {
            var key = PhraseKey(trimmed, words, 0, words.Count, mode);
            if (key == null || CountLetters(key) < minimumLetters)
            {
                return [];
            }

            if (!keys.Contains(key))
            {
                keys.Add(key);
            }
        }

        return keys;
    }

    /// <summary>
    /// The folded key of the words [first, first + count) of a split text, or null when a gap between them does not
    /// allow one name.
    /// </summary>
    public static string? PhraseKey(string text, IReadOnlyList<Match> words, int first, int count, int foldMode)
    {
        var builder = new StringBuilder();
        for (var i = first; i < first + count; i++)
        {
            if (i > first)
            {
                var previous = words[i - 1];
                var gap = GapMarker(text, previous.Index + previous.Length, words[i].Index);
                if (gap == null)
                {
                    return null;
                }

                builder.Append(gap.Value);
            }

            builder.Append(Fold(words[i].Value, foldMode));
        }

        return builder.ToString();
    }

    public static string Fold(string word, int foldMode)
    {
        var lower = word.ToLowerInvariant().Replace("ß", "ss", StringComparison.Ordinal);
        if (foldMode == 0)
        {
            lower = lower
                .Replace("ä", "ae", StringComparison.Ordinal)
                .Replace("ö", "oe", StringComparison.Ordinal)
                .Replace("ü", "ue", StringComparison.Ordinal);
        }

        var decomposed = lower.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c == '’' ? '\'' : c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static int CountLetters(string key) => key.Count(char.IsLetter);

    [GeneratedRegex(@"[\p{L}\p{M}]+(?:['’\-](?![sS](?![\p{L}\p{M}]))[\p{L}\p{M}]+)*", RegexOptions.CultureInvariant)]
    private static partial Regex WordPattern();
}
