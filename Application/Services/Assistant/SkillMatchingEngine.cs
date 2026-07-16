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

using System.Text.Json;
using Klacks.Api.Application.Skills.Meta;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public static class SkillMatchingEngine
{
    public const int GuaranteedMatchCap = 5;

    // Substrings shorter than this over-fire on incidental letter sequences and would flood the
    // guarantee cap with noise.
    private const int MinMatchLength = 4;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

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

            foreach (var keyword in ParseKeywords(skill.TriggerKeywords))
            {
                bestLength = Math.Max(bestLength, CollectMatch(messageLower, keyword, matchedTerms));
            }

            if (skill.Synonyms != null)
            {
                foreach (var synonyms in skill.Synonyms.Values)
                {
                    foreach (var synonym in synonyms)
                    {
                        bestLength = Math.Max(bestLength, CollectMatch(messageLower, synonym, matchedTerms));
                    }
                }
            }

            if (matchedTerms.Count > 0)
            {
                scored.Add((skill.Name, matchedTerms.Count, bestLength, IsReadOnly(skill)));
            }
        }

        return scored
            .OrderByDescending(x => x.DistinctMatches)
            .ThenByDescending(x => x.BestLength)
            .ThenByDescending(x => x.IsReadOnly)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Name)
            .Take(cap)
            .ToList();
    }

    private static int CollectMatch(string messageLower, string? candidate, HashSet<string> matchedTerms)
    {
        var length = MatchLength(messageLower, candidate);
        if (length > 0)
        {
            matchedTerms.Add(candidate!.ToLowerInvariant());
        }

        return length;
    }

    private static int MatchLength(string messageLower, string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate) || candidate.Length < MinMatchLength)
            return 0;

        return messageLower.Contains(candidate.ToLowerInvariant()) ? candidate.Length : 0;
    }

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

    private static IReadOnlyList<string> ParseKeywords(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
