// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Extracts person-name candidates from an assistant answer: pairs of adjacent capitalized words
/// (given name + family name pattern), capped in document order. Single capitalized words are
/// deliberately not candidates — German capitalizes every noun and sentence starts capitalize
/// everything, so single words would flood the resolver. Caseless scripts (CJK, Thai) produce no
/// candidates; that false-negative is documented in the plan.
/// </summary>

using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant.Grounding;

public static class AnswerNameCandidateExtractor
{
    public const int CandidateCap = 20;

    private static readonly Regex CapitalizedPairRegex = new(
        @"(?=(?<first>\p{Lu}[\p{Ll}'’\-]+)[ \t](?<second>\p{Lu}[\p{Ll}'’\-]+))",
        RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public static IReadOnlyList<string> Extract(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var candidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match match in CapitalizedPairRegex.Matches(text))
        {
            var candidate = $"{match.Groups["first"].Value} {match.Groups["second"].Value}";
            var key = AnswerNameMatching.Normalize(candidate);
            if (key.Length == 0 || !seen.Add(key))
            {
                continue;
            }

            candidates.Add(candidate);
            if (candidates.Count >= CandidateCap)
            {
                break;
            }
        }

        return candidates;
    }
}
