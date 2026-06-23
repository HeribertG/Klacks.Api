// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Static utility for Tier1 keyword matching: checks whether any of a skill's trigger keywords
/// or language-specific synonyms appear as a substring in the given user message.
/// Used by the SkillOptimizer evaluation harness.
/// </summary>

using System.Text.Json;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public static class SkillMatchingEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

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
