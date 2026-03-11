// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds LLMResponse objects from provider responses.
/// Parses the [SUGGESTIONS: "..." | "..." | "..."] block embedded by the LLM in its response text.
/// </summary>

using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public class LLMResponseBuilder
{
    private static readonly Regex SuggestionsBlockRegex = new(
        @"\[SUGGESTIONS:\s*(.*?)\]",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex SuggestionQuoteRegex = new(
        @"""([^""]+)""",
        RegexOptions.Compiled);

    public LLMResponse BuildSuccessResponse(
        LLMProviderResponse providerResponse,
        string conversationId,
        string responseContent,
        List<LLMFunctionCall>? allFunctionCalls = null)
    {
        var functionCalls = allFunctionCalls ?? providerResponse.FunctionCalls;

        var (cleanedContent, suggestions) = ExtractSuggestions(responseContent);

        var response = new LLMResponse
        {
            Message = cleanedContent,
            ConversationId = conversationId,
            ActionPerformed = functionCalls.Any(),
            FunctionCalls = functionCalls
                .Select(f => (object)new { f.FunctionName, f.Parameters, f.UiActionSteps })
                .ToList(),
            Usage = new LLMUsageInfo
            {
                InputTokens = providerResponse.Usage.InputTokens,
                OutputTokens = providerResponse.Usage.OutputTokens,
                Cost = providerResponse.Usage.Cost
            },
            Suggestions = suggestions
        };

        response.NavigateTo = ExtractNavigation(cleanedContent);

        return response;
    }

    public LLMResponse BuildErrorResponse(string message)
    {
        return new LLMResponse
        {
            Message = $"❌ {message}",
            Suggestions = new List<string>()
        };
    }

    private static (string CleanedContent, List<string> Suggestions) ExtractSuggestions(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return (content, new List<string>());

        var match = SuggestionsBlockRegex.Match(content);
        if (!match.Success)
            return (content, new List<string>());

        var suggestionsRaw = match.Groups[1].Value;
        var suggestions = new List<string>();

        var quoteMatches = SuggestionQuoteRegex.Matches(suggestionsRaw);
        foreach (Match qm in quoteMatches)
        {
            var suggestion = qm.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(suggestion))
                suggestions.Add(suggestion);

            if (suggestions.Count >= LlmSuggestionFormat.MaxSuggestions)
                break;
        }

        var cleanedContent = SuggestionsBlockRegex.Replace(content, string.Empty).TrimEnd();

        return (cleanedContent, suggestions);
    }

    private static string? ExtractNavigation(string content)
    {
        var navigationMap = new Dictionary<string, string>
        {
            ["dashboard"] = "/dashboard",
            ["clients"] = "/clients",
            ["employees"] = "/clients",
            ["contracts"] = "/contracts",
            ["settings"] = "/settings"
        };

        var lowerContent = content.ToLower();
        foreach (var nav in navigationMap)
        {
            if (lowerContent.Contains($"navigate to {nav.Key}") ||
                lowerContent.Contains($"open {nav.Key}"))
            {
                return nav.Value;
            }
        }

        return null;
    }
}
