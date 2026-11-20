// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Finds personal values that need no dictionary: e-mail addresses and phone numbers. A phone number counts when it starts with + or 0 and has at least nine digits, or when its last
/// digits match a number stored in the communication table. Separators are spaces, hyphens, brackets and slashes; dotted
/// numbers (079.123.45.67) are not recognised, because the dot would turn decimals and dates into phone numbers. Runs before the name matcher, so a name inside an e-mail
/// address does not split the address. Matches never overlap; the earlier pattern in this order wins.
/// </summary>

using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

public static partial class PseudonymPatterns
{
    public static IReadOnlyList<PseudonymPatternMatch> Find(string text, PseudonymDictionary dictionary)
    {
        var matches = new List<PseudonymPatternMatch>();
        Collect(matches, MailPattern().Matches(text), PseudonymKind.Mail);

        foreach (Match match in PhoneCandidatePattern().Matches(text))
        {
            if (IsPhone(match.Value, dictionary))
            {
                TryAdd(matches, new PseudonymPatternMatch(PseudonymKind.Phone, match.Index, match.Length, match.Value));
            }
        }

        matches.Sort((left, right) => left.Index.CompareTo(right.Index));
        return matches;
    }

    private static bool IsPhone(string value, PseudonymDictionary dictionary)
    {
        var digits = PseudonymPhoneNumbers.Digits(value);
        if (dictionary.IsKnownPhone(PseudonymPhoneNumbers.ComparisonTail(value))
            && digits.Length >= PseudonymizationConstants.MinimumKnownPhoneDigits)
        {
            return true;
        }

        return (value.StartsWith('+') || value.StartsWith('0'))
            && digits.Length >= PseudonymizationConstants.MinimumUnknownPhoneDigits;
    }

    private static void Collect(List<PseudonymPatternMatch> target, MatchCollection found, PseudonymKind kind)
    {
        foreach (Match match in found)
        {
            TryAdd(target, new PseudonymPatternMatch(kind, match.Index, match.Length, match.Value));
        }
    }

    private static void TryAdd(List<PseudonymPatternMatch> target, PseudonymPatternMatch candidate)
    {
        var end = candidate.Index + candidate.Length;
        if (target.Any(existing => candidate.Index < existing.Index + existing.Length && existing.Index < end))
        {
            return;
        }

        target.Add(candidate);
    }

    [GeneratedRegex(@"(?<![\w.%+\-])[A-Za-z0-9._%+\-]{1,64}@[A-Za-z0-9\-]{1,63}(?:\.[A-Za-z0-9\-]{1,63}){0,3}\.[A-Za-z]{2,24}(?![\w\-])", RegexOptions.CultureInvariant)]
    private static partial Regex MailPattern();

    [GeneratedRegex(@"(?<![\w+\-./@:])\+?\d[\d \-()/]{5,30}\d(?![\w\-@:/])", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneCandidatePattern();
}
