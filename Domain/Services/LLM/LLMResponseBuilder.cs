using Klacks.Api.Domain.Services.LLM.Providers;
using Klacks.Api.Application.DTOs.LLM;

namespace Klacks.Api.Domain.Services.LLM;

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
                .Select(f => (object)new { f.FunctionName, f.Parameters })
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
            Suggestions = new List<string> { "Hilfe anzeigen", "Erneut versuchen" }
        };
    }

    private string? ExtractNavigation(string content)
    {
        var navigationMap = new Dictionary<string, string>
        {
            ["dashboard"] = "/dashboard",
            ["mitarbeiter"] = "/clients",
            ["verträge"] = "/contracts",
            ["einstellungen"] = "/settings"
        };

        var lowerContent = content.ToLower();
        foreach (var nav in navigationMap)
        {
            if (lowerContent.Contains($"navigiere zu {nav.Key}") ||
                lowerContent.Contains($"öffne {nav.Key}"))
            {
                return nav.Value;
            }
        }

        return null;
    }

    private List<string> GenerateSuggestions(string response)
    {
        var suggestions = new List<string>();

        if (response.ToLower().Contains("mitarbeiter") || response.ToLower().Contains("erstellt"))
        {
            suggestions.Add("Zeige alle Mitarbeiter");
            suggestions.Add("Erstelle weiteren Mitarbeiter");
        }

        if (response.ToLower().Contains("suche") || response.ToLower().Contains("gefunden"))
        {
            suggestions.Add("Erweiterte Suche");
            suggestions.Add("Filter anwenden");
        }

        suggestions.Add("Hilfe anzeigen");
        suggestions.Add("Zum Dashboard");

        return suggestions.Take(4).ToList();
    }
}
