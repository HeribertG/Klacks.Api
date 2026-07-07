// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Removes text-emitted tool-call markup (the antml "&lt;function_calls&gt;/&lt;invoke&gt;" syntax) from
/// assistant messages. This markup is never executed: every provider runs tools via native structured
/// tool_use blocks, so any such block appearing as text is model narration, not a real call. Stripping
/// it before persisting and before replaying history stops the model from imitating its own leaked
/// output (few-shot contamination).
/// </summary>
/// <param name="text">The raw assistant message text that may contain tool-call markup</param>
using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant;

public static partial class ToolCallMarkupSanitizer
{
    public static bool ContainsMarkup(string? text)
    {
        return !string.IsNullOrEmpty(text) && DetectionRegex().IsMatch(text);
    }

    public static string Sanitize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var result = FunctionCallsBlockRegex().Replace(text, string.Empty);
        result = FunctionCallsTrailingRegex().Replace(result, string.Empty);
        result = InvokeBlockRegex().Replace(result, string.Empty);
        result = InvokeTrailingRegex().Replace(result, string.Empty);
        result = ExcessNewlinesRegex().Replace(result, "\n\n");

        return result.Trim();
    }

    [GeneratedRegex(@"<\s*function_calls\b|<\s*invoke\b", RegexOptions.IgnoreCase)]
    private static partial Regex DetectionRegex();

    [GeneratedRegex(@"<\s*function_calls\b[\s\S]*?<\s*/\s*function_calls\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex FunctionCallsBlockRegex();

    [GeneratedRegex(@"<\s*function_calls\b[\s\S]*$", RegexOptions.IgnoreCase)]
    private static partial Regex FunctionCallsTrailingRegex();

    [GeneratedRegex(@"<\s*invoke\b[\s\S]*?<\s*/\s*invoke\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex InvokeBlockRegex();

    [GeneratedRegex(@"<\s*invoke\b[\s\S]*$", RegexOptions.IgnoreCase)]
    private static partial Regex InvokeTrailingRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExcessNewlinesRegex();
}
