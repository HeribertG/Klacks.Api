// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Forward pass of the pseudonymisation for one text. Steps in order: decode JSON \uXXXX escapes of printable
/// characters (tool results arrive as JSON whose default encoder writes "Müller" as Müller and "+41" as +41,
/// so a match on the escaped form would miss exactly where most person data stands); field rules on JSON (personnel
/// number to its P token, birthdate to an age) and on a "#4711" reference to a known personnel number (also the "(#4711)" suffix of name candidate lists); pattern values;
/// finally known names, longest phrase first, on Unicode word boundaries.
/// </summary>

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

public static partial class PseudonymTextPseudonymizer
{
    private const char GenitiveSuffix = 's';

    /// <summary>
    /// The pattern values a text contains, so the caller can allocate their numbers before <see cref="Pseudonymize"/>.
    /// </summary>
    public static IEnumerable<PseudonymPatternMatch> FindPatternValues(string? text, PseudonymDictionary dictionary) =>
        string.IsNullOrEmpty(text) ? [] : PseudonymPatterns.Find(DecodeJsonEscapes(text), dictionary);

    /// <param name="text">The text on its way to the model</param>
    /// <param name="map">Dictionary and pattern numbers; every pattern value of the text must already have a number</param>
    /// <param name="usedBaseTokens">Collects the person tokens that occur, for the legend</param>
    /// <param name="today">Company today, for the age that replaces a birthdate</param>
    /// <param name="requireCapitalSingleWord">True for Klacks-authored system prompts: a single-word name then matches
    /// only when capitalised like the stored name, so a first name such as "Can" or "Will" does not replace every
    /// lowercase "can" in the instructions. Multi-word names match regardless of case.</param>
    public static string Pseudonymize(
        string? text, PseudonymMap map, ISet<string> usedBaseTokens, DateOnly today, bool requireCapitalSingleWord = false)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        var result = DecodeJsonEscapes(text);
        result = ApplyFieldRules(result, map, usedBaseTokens, today);
        result = ReplacePatternValues(result, map);
        return ReplaceNames(result, map, usedBaseTokens, requireCapitalSingleWord);
    }

    internal static string DecodeJsonEscapes(string text)
    {
        if (!text.Contains("\\u", StringComparison.OrdinalIgnoreCase))
        {
            return text;
        }

        return JsonEscapePattern().Replace(text, match =>
        {
            var code = int.Parse(match.Groups["hex"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var c = (char)code;
            if (c == '"' || c == '\\' || char.IsControl(c) || char.IsSurrogate(c))
            {
                return match.Value;
            }

            return match.Groups["slashes"].Value + c;
        });
    }

    private static string ApplyFieldRules(string text, PseudonymMap map, ISet<string> usedBaseTokens, DateOnly today)
    {
        var result = IdNumberFieldPattern().Replace(text, match =>
            TryIdNumberToken(match.Groups["value"].Value, map, usedBaseTokens, out var token)
                ? $"\"{match.Groups["key"].Value}\":\"{token}\""
                : match.Value);

        result = IdNumberReferencePattern().Replace(result, match =>
            TryIdNumberToken(match.Groups["value"].Value, map, usedBaseTokens, out var token)
                ? $"#{token}"
                : match.Value);

        return BirthdateFieldPattern().Replace(result, match =>
        {
            if (!DateOnly.TryParseExact(match.Groups["date"].Value, DateTimeFormats.IsoDate, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var birthdate))
            {
                return match.Value;
            }

            var age = today.Year - birthdate.Year - (today < birthdate.AddYears(today.Year - birthdate.Year) ? 1 : 0);
            var ageText = string.Format(CultureInfo.InvariantCulture, PseudonymizationConstants.BirthdateAgeFormat, age);
            return $"\"{match.Groups["key"].Value}\":\"{ageText}\"";
        });
    }

    private static bool TryIdNumberToken(string digits, PseudonymMap map, ISet<string> usedBaseTokens, out string token)
    {
        token = string.Empty;
        if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var idNumber)
            || !map.Dictionary.TryGetIdNumberToken(idNumber, out token))
        {
            return false;
        }

        usedBaseTokens.Add(token[..token.LastIndexOf(PseudonymizationConstants.TokenSeparator)]);
        map.MarkEmitted(token);
        return true;
    }

    private static string ReplacePatternValues(string text, PseudonymMap map)
    {
        var matches = PseudonymPatterns.Find(text, map.Dictionary);
        if (matches.Count == 0)
        {
            return text;
        }

        var builder = new StringBuilder(text.Length);
        var position = 0;
        foreach (var match in matches)
        {
            if (!map.TryGetPatternToken(match.Kind, match.Value, out var token))
            {
                throw new InvalidOperationException("A pattern value has no pseudonym number; the request is not sent.");
            }

            builder.Append(text, position, match.Index - position).Append(token);
            map.MarkEmitted(token);
            position = match.Index + match.Length;
        }

        return builder.Append(text, position, text.Length - position).ToString();
    }

    private static string ReplaceNames(
        string text, PseudonymMap map, ISet<string> usedBaseTokens, bool requireCapitalSingleWord)
    {
        var dictionary = map.Dictionary;
        if (dictionary.PhraseCount == 0)
        {
            return text;
        }

        var words = PseudonymWords.Split(text);
        var builder = new StringBuilder(text.Length);
        var position = 0;
        var index = 0;
        while (index < words.Count)
        {
            if (IsTokenPrefix(text, words[index]))
            {
                index++;
                continue;
            }

            var (phrase, wordCount, keptSuffix) = LongestPhrase(text, words, index, dictionary);
            if (phrase == null || (requireCapitalSingleWord && wordCount == 1 && IsLowercaseFormOfCapitalName(words[index].Value, phrase)))
            {
                index++;
                continue;
            }

            var start = words[index].Index;
            var last = words[index + wordCount - 1];
            builder.Append(text, position, start - position).Append(phrase.Token);
            usedBaseTokens.Add(phrase.BaseToken);
            map.MarkEmitted(phrase.Token);
            position = last.Index + last.Length - keptSuffix;
            index += wordCount;
        }

        return builder.Append(text, position, text.Length - position).ToString();
    }

    /// <summary>
    /// The longest known phrase starting at the word. A single word ending in s that is no name itself may be a
    /// genitive ("Annas", "Meiers"): then the name without the s is replaced and the s stays in the text.
    /// </summary>
    private static (PseudonymPhrase? Phrase, int WordCount, int KeptSuffix) LongestPhrase(
        string text, IReadOnlyList<Match> words, int index, PseudonymDictionary dictionary)
    {
        var maxWords = Math.Min(PseudonymizationConstants.MaxPhraseWords, words.Count - index);
        for (var count = maxWords; count >= 1; count--)
        {
            for (var mode = 0; mode < PseudonymWords.FoldModeCount; mode++)
            {
                var key = PseudonymWords.PhraseKey(text, words, index, count, mode);
                if (key != null && dictionary.TryGetPhrase(key, out var phrase))
                {
                    return (phrase, count, 0);
                }
            }
        }

        var word = words[index].Value;
        if (word.Length > PseudonymizationConstants.MinimumNameLetters && word[^1] == GenitiveSuffix)
        {
            var stem = word[..^1];
            for (var mode = 0; mode < PseudonymWords.FoldModeCount; mode++)
            {
                if (dictionary.TryGetPhrase(PseudonymWords.Fold(stem, mode), out var phrase))
                {
                    return (phrase, 1, 1);
                }
            }
        }

        return (null, 0, 0);
    }

    private static bool IsLowercaseFormOfCapitalName(string word, PseudonymPhrase phrase) =>
        char.IsLower(word[0]) && phrase.Surface.Length > 0 && char.IsUpper(phrase.Surface[0]);

    private static bool IsTokenPrefix(string text, Match word)
    {
        var end = word.Index + word.Length;
        return end + 1 < text.Length
            && text[end] == PseudonymizationConstants.TokenSeparator
            && char.IsAsciiDigit(text[end + 1])
            && PseudonymPrefixes.TryGetKind(word.Value, out _);
    }

    [GeneratedRegex(@"(?<slashes>(?<!\\)(?:\\\\)*)\\u(?<hex>[0-9A-Fa-f]{4})", RegexOptions.CultureInvariant)]
    private static partial Regex JsonEscapePattern();

    [GeneratedRegex(@"""(?<key>[Ii]d_?[Nn]umber)""\s*:\s*(?<value>\d+)(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex IdNumberFieldPattern();

    [GeneratedRegex(@"(?<![\w#])#(?<value>\d+)(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex IdNumberReferencePattern();

    [GeneratedRegex(@"""(?<key>[Bb]irth_?[Dd]ate)""\s*:\s*""(?<date>\d{4}-\d{2}-\d{2})[^""]*""", RegexOptions.CultureInvariant)]
    private static partial Regex BirthdateFieldPattern();
}
