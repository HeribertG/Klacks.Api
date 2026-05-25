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

    private static readonly Regex RepliesBlockRegex = new(
        @"\[REPLIES:(single|multi|date)(?::([^""]*?))?\s*(.*?)\]",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex RepliesOptionRegex = new(
        @"""([^""]+)""",
        RegexOptions.Compiled);

    public LLMResponse BuildSuccessResponse(
        LLMProviderResponse providerResponse,
        string conversationId,
        string responseContent,
        List<LLMFunctionCall>? allFunctionCalls = null,
        string? navigationRoute = null)
    {
        var functionCalls = allFunctionCalls ?? providerResponse.FunctionCalls;

        var (afterReplies, suggestedReplies) = ExtractSuggestedReplies(responseContent);
        var (cleanedContent, suggestions) = ExtractSuggestions(afterReplies);

        var response = new LLMResponse
        {
            Message = cleanedContent,
            ConversationId = conversationId,
            ActionPerformed = functionCalls.Any(),
            NavigateTo = navigationRoute,
            FunctionCalls = functionCalls
                .Select(f => (object)new { f.FunctionName, f.Parameters, f.UiActionSteps, f.Result })
                .ToList(),
            Usage = new LLMUsageInfo
            {
                InputTokens = providerResponse.Usage.InputTokens,
                OutputTokens = providerResponse.Usage.OutputTokens,
                Cost = providerResponse.Usage.Cost
            },
            Suggestions = suggestions,
            SuggestedReplies = suggestedReplies
        };

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

    private static (string CleanedContent, SuggestedRepliesConfig? Replies) ExtractSuggestedReplies(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return (content, null);

        var match = RepliesBlockRegex.Match(content);
        if (!match.Success)
            return (content, null);

        var mode = match.Groups[1].Value;
        var prompt = match.Groups[2].Success && !string.IsNullOrWhiteSpace(match.Groups[2].Value)
            ? match.Groups[2].Value.Trim()
            : null;
        var optionsRaw = match.Groups[3].Value;

        SuggestedRepliesConfig config;

        if (mode.Equals(LlmRepliesFormat.ModeDate, StringComparison.OrdinalIgnoreCase))
        {
            var labelMatch = RepliesOptionRegex.Match(optionsRaw);
            var datePrompt = labelMatch.Success ? labelMatch.Groups[1].Value.Trim() : null;

            config = new SuggestedRepliesConfig
            {
                SelectionMode = SuggestedReplySelectionModes.Date,
                Prompt = datePrompt,
                Options = new List<SuggestedReply>()
            };
        }
        else
        {
            var options = new List<SuggestedReply>();
            var optionMatches = RepliesOptionRegex.Matches(optionsRaw);
            foreach (Match om in optionMatches)
            {
                var raw = om.Groups[1].Value.Trim();
                var eqIndex = raw.IndexOf('=');
                if (eqIndex > 0)
                {
                    options.Add(new SuggestedReply
                    {
                        Label = raw[..eqIndex].Trim(),
                        Value = raw[(eqIndex + 1)..].Trim()
                    });
                }
                else
                {
                    options.Add(new SuggestedReply { Label = raw, Value = raw });
                }

                if (options.Count >= LlmRepliesFormat.MaxOptions)
                    break;
            }

            if (options.Count == 0)
                return (content, null);

            config = new SuggestedRepliesConfig
            {
                SelectionMode = mode,
                Prompt = prompt,
                Options = options
            };
        }

        var cleanedContent = RepliesBlockRegex.Replace(content, string.Empty).TrimEnd();
        return (cleanedContent, config);
    }

}
