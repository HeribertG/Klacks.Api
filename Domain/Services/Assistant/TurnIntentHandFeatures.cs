// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Cheap deterministic features of a bare chat message for the turn-intent head: question lead and mutation stem as
/// reported by MutationIntentDetector, a trailing question mark (ASCII, full-width and Arabic), digit and date
/// presence, capitalised name-like tokens after the first word (carries no signal in German, where every noun is
/// capitalised) and a token-count bucket. The trainer and the runtime both use this class, so the columns the head
/// was trained on are exactly the columns it sees in production.
/// </summary>
/// <param name="text">The bare user message</param>

using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant;

public static class TurnIntentHandFeatures
{
    private static readonly char[] QuestionMarks = ['?', '？', '؟'];

    private static readonly Regex LeadWord = new(@"^\s*\p{L}+\W*", RegexOptions.Compiled);
    private static readonly Regex DatePattern = new(@"\d{1,2}[./-]\d{1,2}([./-]\d{2,4})?", RegexOptions.Compiled);
    private static readonly Regex WordPattern = new(@"\p{L}[\p{L}\p{M}'-]*", RegexOptions.Compiled);

    private const int CjkRangeStart = 0x3040;
    private const int CjkRangeEnd = 0x9FFF;
    private const int CjkCharactersPerToken = 2;
    private const int TokenBucketOffset = 8;

    private static readonly int[] TokenBucketUpperBounds = [1, 2, 4, 7, 11];

    public static readonly string[] Names =
    [
        "questionLead",
        "endsWithQuestionMark",
        "mutationIntent",
        "mutationStemAfterQuestionLead",
        "hasDigit",
        "hasDatePattern",
        "oneCapitalisedMidSentence",
        "twoOrMoreCapitalisedMidSentence",
        "tokens=1",
        "tokens=2",
        "tokens=3-4",
        "tokens=5-7",
        "tokens=8-11",
        "tokens=12+",
    ];

    public static double[] Extract(string text)
    {
        var trimmed = text.Trim();
        var questionLead = MutationIntentDetector.IsInformationQuestion(trimmed);
        var mutation = MutationIntentDetector.IsMutationIntent(trimmed);
        var stemAfterLead = questionLead
                            && MutationIntentDetector.IsMutationIntent(LeadWord.Replace(trimmed, string.Empty, 1));
        var capitalised = CountCapitalisedAfterFirstWord(trimmed);
        var tokenBucket = TokenBucket(CountTokens(trimmed));

        var features = new double[Names.Length];
        features[0] = Flag(questionLead);
        features[1] = Flag(trimmed.Length > 0 && QuestionMarks.Contains(trimmed[^1]));
        features[2] = Flag(mutation);
        features[3] = Flag(stemAfterLead);
        features[4] = Flag(trimmed.Any(char.IsDigit));
        features[5] = Flag(DatePattern.IsMatch(trimmed));
        features[6] = Flag(capitalised == 1);
        features[7] = Flag(capitalised >= 2);
        features[TokenBucketOffset + tokenBucket] = 1.0;
        return features;
    }

    private static double Flag(bool value) => value ? 1.0 : 0.0;

    private static int CountCapitalisedAfterFirstWord(string text)
    {
        var words = WordPattern.Matches(text);
        var count = 0;
        for (var i = 1; i < words.Count; i++)
        {
            var word = words[i].Value;
            if (word.Length > 1 && char.IsUpper(word[0]) && word.Skip(1).Any(char.IsLower))
            {
                count++;
            }
        }

        return count;
    }

    private static int CountTokens(string text)
    {
        var tokens = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        var hasCjk = text.Any(c => c >= CjkRangeStart && c <= CjkRangeEnd);
        return hasCjk && tokens <= 1 ? Math.Max(1, text.Length / CjkCharactersPerToken) : tokens;
    }

    private static int TokenBucket(int tokens)
    {
        for (var i = 0; i < TokenBucketUpperBounds.Length; i++)
        {
            if (tokens <= TokenBucketUpperBounds[i])
            {
                return i;
            }
        }

        return TokenBucketUpperBounds.Length;
    }
}
