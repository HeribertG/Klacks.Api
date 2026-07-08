// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared stop sequences that prevent models from emitting tool-call XML markup as literal text.
/// </summary>

namespace Klacks.Api.Domain.Services.Assistant.Providers;

public static class LLMStopSequences
{
    // "<function_calls"/"<invoke" is Claude's native tool-call syntax, but any model can imitate it
    // after seeing contaminated history (few-shot contamination). Every provider whose API supports
    // stop sequences applies this list so generation halts at the opening marker instead of leaking
    // the markup to the user. Exception: OpenAI/Azure (OpenAIRequest) — the configured gpt-5.x
    // reasoning models reject the "stop" parameter, so those providers rely on
    // ToolCallMarkupSanitizer and the LLMService guards instead.
    public static readonly IReadOnlyList<string> ToolCallMarkup = ["<function_calls", "<invoke"];

    public static List<string> Merge(List<string>? requestStopSequences)
    {
        var merged = new List<string>(ToolCallMarkup);
        if (requestStopSequences != null)
        {
            merged.AddRange(requestStopSequences.Where(s => !string.IsNullOrEmpty(s) && !merged.Contains(s)));
        }

        return merged;
    }
}
