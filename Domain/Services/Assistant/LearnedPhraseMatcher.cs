// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single source of truth for "this utterance contains a wording the learning loop wrote into
/// skill_phrase". Two callers need the same answer and used to be a copy of each other: the toolset
/// assembler, which turns a hit into a deterministic guarantee, and trajectory capture, which records the
/// hit as attribution for the usefulness quote. If the two rules drifted, the fitness quote would credit a
/// wording that never actually put its skill in front of the model.
/// Matching is plain normalised substring containment, deliberately: a learned wording is a literal
/// sentence fragment a real user typed, not a stemmed keyword. It says the wording occurred, never that it
/// caused the routing.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public static class LearnedPhraseMatcher
{
    /// <summary>
    /// Rows read from skill_phrase per turn. Learned rows are the rarest source in the table by orders of
    /// magnitude, so this is a ceiling against a runaway learner, not a working limit.
    /// </summary>
    public const int MatchLimit = 200;

    /// <summary>
    /// How many skills a learned wording may guarantee at once. Guaranteed skills survive the provider cap
    /// ahead of retrieved ones, so an unbounded number of them would let the learning loop crowd out the
    /// retrieval result it is supposed to complement.
    /// </summary>
    public const int GuaranteeCap = 2;

    /// <summary>
    /// Shortest wording that may claim a guarantee slot. Mirrors SkillMatchingEngine.MinMatchLength: below
    /// it, raw containment fires on incidental letter sequences. The learning loop generates whole phrases
    /// and never gets near this bound, but the admin card can rewrite a learned row to anything.
    /// </summary>
    private const int MinGuaranteedPhraseLength = 4;

    /// <summary>
    /// Owner names of every learned wording that literally occurs in the message, most specific first.
    /// Used for the deterministic guarantee, so it filters on owner kind and on a minimum length -
    /// unlike FirstMatchingOwner, which only attributes and must stay as permissive as it always was.
    /// </summary>
    /// <param name="learnedPhrases">Active phrases of source Learned</param>
    /// <param name="message">Raw user message of the current turn</param>
    /// <param name="ownerKind">Owner kind to keep, see SkillPhraseOwnerKinds</param>
    /// <param name="cap">Maximum number of owner names to return</param>
    public static IReadOnlyList<string> MatchingOwnerNames(
        IEnumerable<SkillPhrase> learnedPhrases, string? message, string ownerKind, int cap)
    {
        var normalized = MessageNormalizer.Normalize(message);
        if (normalized.Length == 0 || cap <= 0)
        {
            return [];
        }

        return learnedPhrases
            .Where(phrase => string.Equals(phrase.OwnerKind, ownerKind, StringComparison.Ordinal))
            .Where(phrase => !string.IsNullOrWhiteSpace(phrase.OwnerName))
            .Select(phrase => (phrase.OwnerName, Needle: MessageNormalizer.Normalize(phrase.Phrase)))
            .Where(candidate => candidate.Needle.Length >= MinGuaranteedPhraseLength)
            .Where(candidate => normalized.Contains(candidate.Needle, StringComparison.Ordinal))
            // Longest wording first, the same "most specific match wins" rule the keyword guarantee uses;
            // the owner name breaks ties so the same message always yields the same guarantee.
            .OrderByDescending(candidate => candidate.Needle.Length)
            .ThenBy(candidate => candidate.OwnerName, StringComparer.OrdinalIgnoreCase)
            .Select(candidate => candidate.OwnerName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(cap)
            .ToList();
    }

    /// <summary>
    /// Owner name of the first learned wording that occurs in the message, or null. Attribution only:
    /// every owner kind counts and there is no length floor, so the recorded hit keeps meaning exactly
    /// what it meant before this helper existed.
    /// </summary>
    /// <param name="learnedPhrases">Active phrases of source Learned</param>
    /// <param name="message">Raw user message of the turn being recorded</param>
    public static string? FirstMatchingOwner(IEnumerable<SkillPhrase> learnedPhrases, string? message)
    {
        var normalized = MessageNormalizer.Normalize(message);
        if (normalized.Length == 0)
        {
            return null;
        }

        foreach (var phrase in learnedPhrases)
        {
            if (string.IsNullOrWhiteSpace(phrase.Phrase))
            {
                continue;
            }

            if (normalized.Contains(MessageNormalizer.Normalize(phrase.Phrase), StringComparison.Ordinal))
            {
                return phrase.OwnerName;
            }
        }

        return null;
    }
}
