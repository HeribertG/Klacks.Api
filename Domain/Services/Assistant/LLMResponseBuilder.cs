// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public class LLMResponseBuilder
{
    public LLMResponse BuildSuccessResponse(
        LLMProviderResponse providerResponse,
        string conversationId,
        string responseContent,
        List<LLMFunctionCall>? allFunctionCalls = null)
    {
        var functionCalls = allFunctionCalls ?? providerResponse.FunctionCalls;
        var response = new LLMResponse
        {
            Message = responseContent,
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
            }
        };

        response.NavigateTo = ExtractNavigation(responseContent);
        response.Suggestions = GenerateSuggestions(responseContent);

        return response;
    }

    public LLMResponse BuildErrorResponse(string message)
    {
        return new LLMResponse
        {
            Message = $"❌ {message}",
            Suggestions = new List<string> { "Show help", "Try again" }
        };
    }

    private string? ExtractNavigation(string content)
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

    private List<string> GenerateSuggestions(string response)
    {
        var suggestions = new List<string>();

        if (response.ToLower().Contains("employee") || response.ToLower().Contains("created"))
        {
            suggestions.Add("Show all employees");
            suggestions.Add("Create another employee");
        }

        if (response.ToLower().Contains("search") || response.ToLower().Contains("found"))
        {
            suggestions.Add("Advanced search");
            suggestions.Add("Apply filter");
        }

        suggestions.Add("Show help");
        suggestions.Add("Go to dashboard");

        return suggestions.Take(4).ToList();
    }
}
