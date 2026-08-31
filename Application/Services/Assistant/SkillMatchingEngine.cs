// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Static utility for Tier1 keyword matching: checks whether any of a skill's trigger keywords
/// or synonyms appear as a substring in the given user message. Powers the deterministic
/// keyword guarantee in both skill-selection pipelines (LLMStreamingOrchestrator and
/// ProcessLLMMessageCommand): skills literally named in the message are always in the tool set,
/// independent of embedding ranking — weak models must not depend on probabilistic retrieval.
/// Ranking under the guarantee cap: number of distinct matched terms first, then longest matched
/// term, then read-only skills before mutating ones (per SkillRiskClassifier), then skill name as
/// the final deterministic tiebreak.
/// </summary>

using Klacks.Api.Application.Skills.Meta;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public static class SkillMatchingEngine
{
    public const int GuaranteedMatchCap = 5;

    // Substrings shorter than this over-fire on incidental letter sequences and would flood the
    // guarantee cap with noise.
    private const int MinMatchLength = 4;

    // Below MinMatchLength a phrase still matches, but only as a whole word. Measured 2026-07-31: the
    // only anchors that survive in non-Latin-script languages are two- and three-letter abbreviations
    // (api, llm, sso, xml, erp, mcp, stt, ai, id), because translators keep exactly those in Latin
    // script while translating everything else. Nine of the twenty-three globally valid anchors sit
    // below the substring threshold, so a plain length cut-off silences the very terms that work
    // everywhere. Raw containment cannot be used for them - "api" occurs inside "rapide", "id" inside
    // "Idee" - hence the boundary rule.
    private const int MinAnchorMatchLength = 2;


    private static readonly SkillRiskClassifier RiskClassifier = new();

    public static IReadOnlyList<string> TopKeywordMatchedSkillNames(
        IEnumerable<AgentSkill> skills, string userMessage, int cap = GuaranteedMatchCap)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            return [];

        var messageLower = userMessage.ToLowerInvariant();
        var scored = new List<(string Name, int DistinctMatches, int BestLength, bool IsReadOnly)>();

        foreach (var skill in skills)
        {
            var matchedTerms = new HashSet<string>(StringComparer.Ordinal);
            var bestLength = 0;

            foreach (var group in TriggerKeywordFormat.ReadGrouped(skill.TriggerKeywords))
            {
                var isAnchor = IsAnchorLanguage(group.Key);
                foreach (var keyword in group.Value)
                {
                    bestLength = Math.Max(bestLength, CollectMatch(messageLower, keyword, matchedTerms, isAnchor));
                }
            }

            if (skill.Synonyms != null)
            {
                foreach (var synonyms in skill.Synonyms.Values)
                {
                    foreach (var synonym in synonyms)
                    {
                        bestLength = Math.Max(bestLength, CollectMatch(messageLower, synonym, matchedTerms, isAnchor: false));
                    }
                }
            }

            if (matchedTerms.Count > 0)
            {
                scored.Add((skill.Name, matchedTerms.Count, bestLength, IsReadOnly(skill)));
            }
        }

        // W4.3: the read-before-mutate tiebreak is correct for lookups ("zeig mir den Dienstplan"),
        // but on a mutation intent ("Bitte Spamregel neu anlegen.") it hands the same guarantee rank
        // to the read counterpart and the model then lists instead of creating. For mutation intents
        // the mutating skills rank first, so the create/update/delete skill is never displaced by its
        // list/get sibling under the guarantee cap.
        var preferMutating = Klacks.Api.Domain.Services.Assistant.MutationIntentDetector.IsMutationIntent(userMessage);

        return scored
            .OrderByDescending(x => x.DistinctMatches)
            .ThenByDescending(x => x.BestLength)
            .ThenBy(x => preferMutating ? x.IsReadOnly : !x.IsReadOnly)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Name)
            .Take(cap)
            .ToList();
    }

    /// <summary>
    /// Only phrases stored under the language-neutral tag are anchors. A short phrase from a real
    /// language is a filler word ("ein", "ab", "der") and must stay silenced; a legacy flat keyword
    /// array reads as Undetermined and is therefore NOT treated as an anchor either.
    /// </summary>
    private static bool IsAnchorLanguage(string language) =>
        string.Equals(language, SkillPhraseLanguages.Multiple, StringComparison.OrdinalIgnoreCase);

    private static int CollectMatch(string messageLower, string? candidate, HashSet<string> matchedTerms, bool isAnchor)
    {
        var length = MatchLength(messageLower, candidate, isAnchor);
        if (length > 0)
        {
            matchedTerms.Add(candidate!.ToLowerInvariant());
        }

        return length;
    }

    private static int MatchLength(string messageLower, string? candidate, bool isAnchor)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return 0;

        var needle = candidate.ToLowerInvariant();

        if (needle.Length >= MinMatchLength)
            return messageLower.Contains(needle) ? needle.Length : 0;

        if (!isAnchor || needle.Length < MinAnchorMatchLength)
            return 0;

        return ContainsAsWholeWord(messageLower, needle) ? needle.Length : 0;
    }

    /// <summary>
    /// Whole-word containment for short anchors. Only a LATIN letter or digit counts as a boundary
    /// breaker: a Japanese, Thai, Greek or Arabic character next to a Latin anchor IS a word boundary,
    /// so "apiキー" matches "api" while "rapide" does not. Using char.IsLetterOrDigit instead would
    /// treat the Japanese character as a continuation and silence the anchor in exactly the languages
    /// it was kept for.
    /// </summary>
    /// <param name="haystack">Already lower-cased user message.</param>
    /// <param name="needle">Already lower-cased anchor, shorter than MinMatchLength.</param>
    private static bool ContainsAsWholeWord(string haystack, string needle)
    {
        var start = 0;

        while (start <= haystack.Length - needle.Length)
        {
            var index = haystack.IndexOf(needle, start, StringComparison.Ordinal);
            if (index < 0)
                return false;

            var startsCleanly = index == 0 || !IsLatinWordCharacter(haystack[index - 1]);
            var end = index + needle.Length;
            var endsCleanly = end >= haystack.Length || !IsLatinWordCharacter(haystack[end]);

            if (startsCleanly && endsCleanly)
                return true;

            start = index + 1;
        }

        return false;
    }

    private static bool IsLatinWordCharacter(char c) =>
        char.IsAsciiLetterOrDigit(c) || (c >= 'À' && c <= 'ɏ' && char.IsLetter(c));

    /// <summary>
    /// True when the skill only reads. Classifies via SkillRiskClassifier so the ranking preference
    /// for read-only skills agrees with the autonomy gate, instead of re-deriving "writes to the
    /// database" from name prefixes and drifting apart from it.
    /// </summary>
    private static bool IsReadOnly(AgentSkill skill)
    {
        var category = Enum.TryParse<SkillCategory>(skill.Category, ignoreCase: true, out var parsed)
            ? parsed
            : SkillCategory.Action;

        var descriptor = new SkillDescriptor(
            Name: skill.Name,
            Description: string.Empty,
            Category: category,
            Parameters: Array.Empty<SkillParameter>(),
            RequiredPermissions: Array.Empty<string>(),
            RequiredCapabilities: Array.Empty<LLMCapability>(),
            ImplementationType: null);

        return RiskClassifier.Classify(descriptor) == SkillRiskClass.ReadOnly;
    }

    // A multiword trigger phrase found verbatim in the message is a strong, deterministic signal
    // that the user named this skill's action explicitly (single words over-fire on incidental
    // vocabulary). Used by CompetingSkillIntentDetector to spot skill intents inside messages a
    // recipe trigger has hijacked.
    public static IReadOnlyList<string> MatchedMultiwordPhrases(AgentSkill skill, string userMessage, string? language)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            return [];

        var messageLower = userMessage.ToLowerInvariant();
        var phrases = new List<string>();

        foreach (var keyword in ParseKeywords(skill.TriggerKeywords))
        {
            CollectMultiwordPhrase(messageLower, keyword, phrases);
        }

        if (!string.IsNullOrEmpty(language) && skill.Synonyms != null)
        {
            foreach (var entry in skill.Synonyms)
            {
                if (!string.Equals(entry.Key, language, StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var synonym in entry.Value)
                {
                    CollectMultiwordPhrase(messageLower, synonym, phrases);
                }
            }
        }

        return phrases;
    }

    private static void CollectMultiwordPhrase(string messageLower, string? candidate, List<string> phrases)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return;

        var trimmed = candidate.Trim();
        if (trimmed.Length < MinMatchLength || !trimmed.Contains(' '))
            return;

        if (messageLower.Contains(trimmed.ToLowerInvariant()))
        {
            phrases.Add(trimmed);
        }
    }

    public static bool MatchesSkillKeywords(AgentSkill skill, string userMessage, string language)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            return false;

        var messageLower = userMessage.ToLowerInvariant();

        var keywords = ParseKeywords(skill.TriggerKeywords);
        foreach (var keyword in keywords)
        {
            if (!string.IsNullOrWhiteSpace(keyword) &&
                messageLower.Contains(keyword.ToLowerInvariant()))
                return true;
        }

        if (skill.Synonyms != null &&
            skill.Synonyms.TryGetValue(language, out var synonyms))
        {
            foreach (var synonym in synonyms)
            {
                if (!string.IsNullOrWhiteSpace(synonym) &&
                    messageLower.Contains(synonym.ToLowerInvariant()))
                    return true;
            }
        }

        return false;
    }

    // Language-blind on purpose: every caller here searches the message for a literal substring, and
    // a phrase of the wrong language simply does not occur in it. The same holds for the synonyms the
    // callers iterate across all languages, so grouping the keywords by language changes nothing here.
    private static IReadOnlyList<string> ParseKeywords(string? json) =>
        TriggerKeywordFormat.ReadFlat(json);
}
