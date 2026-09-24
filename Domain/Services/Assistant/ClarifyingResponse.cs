// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Recognizes an assistant answer that asks the user for input instead of claiming a completed action: a
/// clarifying question or an interactive reply affordance ("[REPLIES:date …]"). Shared by the no-action
/// guards of both chat loops and by the empty-answer recovery, so a question never trips a no-action
/// correction on one path while passing on the other.
/// </summary>
/// <param name="content">The assistant answer to classify; blank never counts as a question.</param>
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

internal static class ClarifyingResponse
{
    private const string QuestionMark = "?";

    internal static bool IsClarifying(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        return content.TrimEnd().EndsWith(QuestionMark, StringComparison.Ordinal)
            || content.Contains(LlmRepliesFormat.BlockPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
