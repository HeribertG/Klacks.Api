// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Static engine for Tier1 skill matching via keywords and synonyms.
/// Used by ProcessLLMMessageCommand, LLMStreamingOrchestrator, and SkillOptimizer.
/// </summary>

using System.Text.Json;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public static class SkillMatchingEngine
{
    private const int WordBoundaryThreshold = 5;

    public static bool MatchesSkillKeywords(AgentSkill skill, string userMessage, string? language)
    {
        var messageLower = userMessage.ToLowerInvariant();

        if (MatchesSynonyms(skill.Synonyms, messageLower, language))
            return true;

        return MatchesLegacyTriggerKeywords(skill.TriggerKeywords, messageLower);
    }

    public static bool MatchesSynonyms(
        Dictionary<string, List<string>>? synonyms,
        string messageLower,
        string? language)
    {
        if (synonyms == null || synonyms.Count == 0)
            return false;

        var languagesToCheck = GetLanguagePriority(language);

        foreach (var lang in languagesToCheck)
        {
            if (!synonyms.TryGetValue(lang, out var keywords) || keywords.Count == 0)
                continue;

            if (keywords.Any(kw => MatchesKeyword(messageLower, kw.ToLowerInvariant())))
                return true;
        }

        return false;
    }

    public static bool MatchesLegacyTriggerKeywords(string triggerKeywords, string messageLower)
    {
        if (string.IsNullOrWhiteSpace(triggerKeywords) || triggerKeywords == "[]")
            return false;

        try
        {
            var keywords = JsonSerializer.Deserialize<List<string>>(triggerKeywords);
            if (keywords == null || keywords.Count == 0) return false;

            return keywords.Any(kw => MatchesKeyword(messageLower, kw.ToLowerInvariant()));
        }
        catch
        {
            return false;
        }
    }

    public static bool MatchesKeyword(string message, string keyword)
    {
        if (keyword.Length < WordBoundaryThreshold)
        {
            return Regex.IsMatch(message, $@"\b{Regex.Escape(keyword)}\b", RegexOptions.IgnoreCase);
        }

        return message.Contains(keyword);
    }

    public static List<string> GetLanguagePriority(string? language)
    {
        var lang = (language ?? "de").ToLowerInvariant();

        return lang switch
        {
            "de" => ["de", "en"],
            "en" => ["en", "de"],
            "fr" => ["fr", "en", "de"],
            "it" => ["it", "en", "de"],
            _ => [lang, "en", "de"]
        };
    }
}
