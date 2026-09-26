// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides whether a sentence that CompletionClaimDetector flags as a completion claim is in fact a negation of
/// it ("Sie haben noch keinen Wert gespeichert", "nothing has been saved yet", "rien n'a été enregistré",
/// "non è stato salvato nulla"). Live 2026-09-26 such an honest sentence after a read-only recipe triggered the
/// nothing-stored notice. Core languages only (de/en/fr/it); it is applied by that notice alone, the no-action
/// notice keeps the plain detector.
///
/// Biased towards keeping claims: a sentence counts as negated only when it contains at least one core completion
/// word AND every such word has a negation word within its own clause - up to NegationWindow words before it or
/// FollowingNegationWindow words after it (German and Italian also negate behind the participle: "gespeichert ist
/// nichts", "salvato niente"). Clauses
/// end at , ; : dashes and at coordinating conjunctions (und/and/et/e, aber/but/mais/ma, sowie, jedoch), so a
/// negation in one clause never cancels a claim in the next ("Ich habe keine Fehler gefunden und den Nachlauf
/// gespeichert" stays a claim). "nicht nur" / "not only" / "pas seulement" / "non solo" are not negations. A claim
/// matched only through plugin phrases has no core completion word and therefore is never treated as negated.
///
/// DeniesCompletion answers the question for a whole answer: at least one sentence negates a completion and no
/// sentence claims one. The no-action notice and the non-streaming tool nudge use it (live 2026-09-26: after the
/// honest "Nein – gespeichert ist nichts" the notice "Es wurde keine Aktion ausgeführt – bitte die Anfrage erneut
/// stellen" was appended, because the user's yes/no question contained "gespeichert" and counted as a mutation
/// request).
/// </summary>
/// <param name="sentence">One sentence of the assistant answer.</param>

using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant;

internal static class ClaimNegationDetector
{
    private const int NegationWindow = 6;
    private const int FollowingNegationWindow = 3;
    private const string EnglishContractionSuffix = "n't";
    private const string EnglishTypographicContractionSuffix = "n’t";
    private const string FrenchElidedNegation = "n'";
    private const string FrenchTypographicElidedNegation = "n’";
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(250);

    private static readonly Regex SentenceBoundary = new(
        @"(?<=[.!?])\s+|[。！？\n]+", RegexOptions.CultureInvariant, MatchTimeout);

    private static readonly Regex ClauseBoundary = new(
        @"[,;:–—]|\s-\s|\b(?:und|and|et|e|aber|but|mais|ma|sowie|jedoch)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, MatchTimeout);

    private static readonly Regex WordPattern = new(
        @"\p{L}+(?:['’]\p{L}+)*", RegexOptions.CultureInvariant, MatchTimeout);

    private static readonly HashSet<string> NegationWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "nicht", "nichts", "kein", "keine", "keinen", "keinem", "keiner", "keines", "nie", "niemals",
        "not", "no", "never", "nothing", "none", "nor",
        "ne", "pas", "jamais", "rien", "aucun", "aucune",
        "non", "nessun", "nessuno", "nessuna", "mai", "niente", "nulla",
    };

    private static readonly HashSet<string> RestrictiveFollowers = new(StringComparer.OrdinalIgnoreCase)
    {
        "nur", "only", "seulement", "solo", "soltanto", "solamente",
    };

    /// <summary>
    /// True when the answer denies having completed something and claims nothing anywhere: some sentence is a
    /// negated completion and no sentence is a completion claim that is not negated.
    /// </summary>
    /// <param name="response">The whole assistant answer.</param>
    internal static bool DeniesCompletion(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return false;
        }

        var sentences = SentenceBoundary.Split(response).Where(sentence => !string.IsNullOrWhiteSpace(sentence)).ToList();
        return sentences.Any(IsNegated)
            && !sentences.Any(sentence => CompletionClaimDetector.ClaimsCompletion(sentence) && !IsNegated(sentence));
    }

    internal static bool IsNegated(string sentence)
    {
        var completionWords = 0;
        foreach (var clause in ClauseBoundary.Split(sentence))
        {
            var words = WordPattern.Matches(clause).Select(match => match.Value.ToLowerInvariant()).ToList();
            for (var index = 0; index < words.Count; index++)
            {
                if (!IsCompletionWord(words[index]))
                {
                    continue;
                }

                completionWords++;
                if (!HasNegationNear(words, index))
                {
                    return false;
                }
            }
        }

        return completionWords > 0;
    }

    private static bool IsCompletionWord(string word) =>
        CompletionClaimDetector.IsCoreCompletionWord(word)
        || word.Split('\'', '’').Any(CompletionClaimDetector.IsCoreCompletionWord);

    private static bool HasNegationNear(IReadOnlyList<string> words, int completionIndex)
    {
        var first = Math.Max(0, completionIndex - NegationWindow);
        var last = Math.Min(words.Count - 1, completionIndex + FollowingNegationWindow);
        for (var index = first; index <= last; index++)
        {
            if (index == completionIndex)
            {
                continue;
            }

            if (IsNegation(words[index]) && !IsRestrictive(words, index))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNegation(string word) =>
        NegationWords.Contains(word)
        || word.EndsWith(EnglishContractionSuffix, StringComparison.Ordinal)
        || word.EndsWith(EnglishTypographicContractionSuffix, StringComparison.Ordinal)
        || word.StartsWith(FrenchElidedNegation, StringComparison.Ordinal)
        || word.StartsWith(FrenchTypographicElidedNegation, StringComparison.Ordinal);

    private static bool IsRestrictive(IReadOnlyList<string> words, int negationIndex) =>
        negationIndex + 1 < words.Count && RestrictiveFollowers.Contains(words[negationIndex + 1]);
}
