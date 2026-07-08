// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Static utility for Tier1 keyword matching: checks whether any of a skill's trigger keywords
/// or synonyms appear as a substring in the given user message. Powers the deterministic
/// keyword guarantee in both skill-selection pipelines (skills literally named in the message are
/// always in the tool set, independent of embedding ranking — weak models must not depend on
/// probabilistic retrieval) and the SkillOptimizer evaluation harness.
/// </summary>

using System.Text.Json;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public static class SkillMatchingEngine
{
    public const int GuaranteedMatchCap = 5;

    // Substrings shorter than this over-fire on incidental letter sequences and would flood the
    // guarantee cap with noise.
    private const int MinMatchLength = 4;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static IReadOnlyList<string> TopKeywordMatchedSkillNames(
        IEnumerable<AgentSkill> skills, string userMessage, int cap = GuaranteedMatchCap)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            return [];

        var messageLower = userMessage.ToLowerInvariant();
        var scored = new List<(string Name, int Score)>();

        foreach (var skill in skills)
        {
            var best = 0;

            foreach (var keyword in ParseKeywords(skill.TriggerKeywords))
            {
                best = Math.Max(best, MatchLength(messageLower, keyword));
            }

            if (skill.Synonyms != null)
            {
                foreach (var synonyms in skill.Synonyms.Values)
                {
                    foreach (var synonym in synonyms)
                    {
                        best = Math.Max(best, MatchLength(messageLower, synonym));
                    }
                }
            }

            if (best > 0)
            {
                scored.Add((skill.Name, best));
            }
        }

        return scored
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Name)
            .Take(cap)
            .ToList();
    }

    private static int MatchLength(string messageLower, string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate) || candidate.Length < MinMatchLength)
            return 0;

        return messageLower.Contains(candidate.ToLowerInvariant()) ? candidate.Length : 0;
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
