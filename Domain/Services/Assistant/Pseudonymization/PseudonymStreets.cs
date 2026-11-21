// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Street addresses for the pseudonymisation: parses the street-and-number part of a stored address line into folded
/// keys and finds those keys in a text. A text is split into words (as for names) and house numbers (12, 12a, 12-14,
/// 12/1); two of them belong to one street only when whitespace and at most one full stop separate them, so
/// "Bahnhofstr. 12" and "St. Gallerstrasse 4" connect, while a comma, quote or colon ends the street. A street is up to
/// <see cref="PseudonymizationConstants.MaxStreetWords"/> words followed by its number ("Bahnhofstrasse 12") or a number
/// of at most three digits followed by its words ("12 rue de Lausanne", never a postcode and town such as "9472 Grabs");
/// a street name without a number is never a key. Folding is the name
/// folding (case, accents, ß, both umlaut forms) plus the street suffix strasse/str and a few French abbreviations.
/// </summary>

using System.Text;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

public static partial class PseudonymStreets
{
    private const char Dot = '.';
    private const char KeySeparator = ' ';
    private const string NumberGroup = "num";

    /// <summary>
    /// The street-and-number part of a stored line, or null when the line has no house number next to a street name.
    /// A street followed by its number wins over a number followed by words, so "3. Stock" in a later part of the
    /// line never becomes the key.
    /// </summary>
    public static PseudonymStreetLine? Parse(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var elements = Tokenize(line);
        for (var i = 0; i < elements.Count; i++)
        {
            var before = IsNumber(elements[i]) ? Math.Min(WordsBefore(line, elements, i, 0), PseudonymizationConstants.MaxStreetWords) : 0;
            if (before > 0)
            {
                return Line(line, elements, i - before, i + 1, i);
            }
        }

        for (var i = 0; i < elements.Count; i++)
        {
            var after = IsLeadingHouseNumber(elements[i])
                ? Math.Min(WordsAfter(line, elements, i), PseudonymizationConstants.MaxStreetWords)
                : 0;
            if (after > 0)
            {
                return Line(line, elements, i, i + 1 + after, i);
            }
        }

        return null;
    }

    /// <summary>
    /// The known street addresses of a text, left to right, never overlapping; the longest street wins.
    /// </summary>
    public static IReadOnlyList<PseudonymStreetMatch> Find(string text, PseudonymDictionary dictionary)
    {
        var matches = new List<PseudonymStreetMatch>();
        if (dictionary.StreetCount == 0 || string.IsNullOrEmpty(text))
        {
            return matches;
        }

        var elements = Tokenize(text);
        var firstFree = 0;
        for (var i = 0; i < elements.Count; i++)
        {
            if (!IsNumber(elements[i]) || !dictionary.IsStreetHouseNumber(NormalizeNumber(elements[i].Value)))
            {
                continue;
            }

            var number = i;
            var before = Math.Min(WordsBefore(text, elements, number, firstFree), PseudonymizationConstants.MaxStreetWords);
            var after = IsLeadingHouseNumber(elements[number])
                ? Math.Min(WordsAfter(text, elements, number), PseudonymizationConstants.MaxStreetWords)
                : 0;
            if (TryWindow(elements, dictionary, before, count => (number - count, number + 1), out var match, out var end)
                || TryWindow(elements, dictionary, after, count => (number, number + 1 + count), out match, out end))
            {
                matches.Add(match);
                firstFree = end;
                i = end - 1;
            }
        }

        return matches;
    }

    private static bool TryWindow(
        IReadOnlyList<Match> elements,
        PseudonymDictionary dictionary,
        int maxWords,
        Func<int, (int Start, int End)> window,
        out PseudonymStreetMatch match,
        out int end)
    {
        for (var count = maxWords; count >= 1; count--)
        {
            var (start, stop) = window(count);
            for (var mode = 0; mode < PseudonymWords.FoldModeCount; mode++)
            {
                if (dictionary.TryGetStreet(Key(elements, start, stop, mode), out var phrase))
                {
                    var from = elements[start].Index;
                    var last = elements[stop - 1];
                    match = new PseudonymStreetMatch(from, last.Index + last.Length - from, phrase);
                    end = stop;
                    return true;
                }
            }
        }

        match = default;
        end = 0;
        return false;
    }

    private static PseudonymStreetLine? Line(string line, IReadOnlyList<Match> elements, int start, int end, int number)
    {
        var letters = 0;
        for (var i = start; i < end; i++)
        {
            if (!IsNumber(elements[i]))
            {
                letters += elements[i].Value.Count(char.IsLetter);
            }
        }

        if (letters < PseudonymizationConstants.MinimumNameLetters)
        {
            return null;
        }

        var keys = new List<string>(PseudonymWords.FoldModeCount);
        for (var mode = 0; mode < PseudonymWords.FoldModeCount; mode++)
        {
            var key = Key(elements, start, end, mode);
            if (!keys.Contains(key))
            {
                keys.Add(key);
            }
        }

        var from = elements[start].Index;
        var last = elements[end - 1];
        return new PseudonymStreetLine(
            keys, line[from..(last.Index + last.Length)], NormalizeNumber(elements[number].Value));
    }

    /// <summary>
    /// How many word elements directly before the number connect to it, not reaching below the first free element.
    /// </summary>
    private static int WordsBefore(string text, IReadOnlyList<Match> elements, int number, int firstFree)
    {
        var count = 0;
        for (var i = number - 1; i >= firstFree; i--)
        {
            if (IsNumber(elements[i]) || !IsStreetGap(text, elements[i], elements[i + 1]))
            {
                break;
            }

            count++;
        }

        return count;
    }

    private static int WordsAfter(string text, IReadOnlyList<Match> elements, int number)
    {
        var count = 0;
        for (var i = number + 1; i < elements.Count; i++)
        {
            if (IsNumber(elements[i]) || !IsStreetGap(text, elements[i - 1], elements[i]))
            {
                break;
            }

            count++;
        }

        return count;
    }

    private static bool IsStreetGap(string text, Match left, Match right)
    {
        var start = left.Index + left.Length;
        if (right.Index <= start)
        {
            return false;
        }

        var dotSeen = false;
        for (var i = start; i < right.Index; i++)
        {
            var c = text[i];
            if (c == Dot && !dotSeen)
            {
                dotSeen = true;
                continue;
            }

            if (!char.IsWhiteSpace(c))
            {
                return false;
            }
        }

        return true;
    }

    private static string Key(IReadOnlyList<Match> elements, int start, int end, int foldMode)
    {
        var builder = new StringBuilder();
        for (var i = start; i < end; i++)
        {
            if (i > start)
            {
                builder.Append(KeySeparator);
            }

            builder.Append(IsNumber(elements[i])
                ? NormalizeNumber(elements[i].Value)
                : NormalizeWord(elements[i].Value, foldMode));
        }

        return builder.ToString();
    }

    private static string NormalizeWord(string word, int foldMode)
    {
        var folded = PseudonymWords.Fold(word, foldMode);
        if (PseudonymizationConstants.StreetWordAbbreviations.TryGetValue(folded, out var abbreviation))
        {
            return abbreviation;
        }

        return folded.EndsWith(PseudonymizationConstants.StreetSuffixFull, StringComparison.Ordinal)
            ? folded[..^PseudonymizationConstants.StreetSuffixFull.Length] + PseudonymizationConstants.StreetSuffixShort
            : folded;
    }

    private static string NormalizeNumber(string number) => number.ToLowerInvariant();

    private static bool IsNumber(Match element) => element.Groups[NumberGroup].Success;

    private static bool IsLeadingHouseNumber(Match element) =>
        IsNumber(element)
        && element.Value.TakeWhile(char.IsAsciiDigit).Count() <= PseudonymizationConstants.MaxLeadingHouseNumberDigits;

    private static IReadOnlyList<Match> Tokenize(string text) => ElementPattern().Matches(text);

    [GeneratedRegex(
        @"(?<num>(?<![\p{L}\p{N}])\d{1,5}(?:[\-/]\d{1,5})?[A-Za-z]?(?![\p{L}\p{N}]))|(?<word>[\p{L}\p{M}]+(?:['’\-](?![sS](?![\p{L}\p{M}]))[\p{L}\p{M}]+)*)",
        RegexOptions.CultureInvariant)]
    private static partial Regex ElementPattern();
}
