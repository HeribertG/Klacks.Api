// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

/// <summary>
/// Decides what an OpenAI-compatible reasoning model's effective answer is, given the regular
/// content, the reasoning_content channel, and whether the turn produced tool calls. Reasoning models
/// stream thinking into reasoning_content; it is the ANSWER only when there is no content and no tool
/// call. One symmetric rule for both the non-stream and (buffered) stream paths so chain-of-thought
/// never leaks into the chat on a normal or tool-calling turn.
/// </summary>
public static class ReasoningContentResolver
{
    /// <summary>
    /// Resolves the answer AND reports whether it had to be taken from the reasoning channel. There
    /// is deliberately no content-only overload: a caller that must never show raw chain-of-thought
    /// (e.g. the opening greeting) needs FromReasoning to treat such an answer as a failure, and a
    /// convenience wrapper that drops the flag is exactly the trap a future provider would fall into.
    /// </summary>
    /// <param name="content">The regular content channel (may be empty)</param>
    /// <param name="reasoning">The reasoning_content channel (may be empty)</param>
    /// <param name="hasToolCalls">True when the turn produced one or more tool calls</param>
    public static ResolvedAnswer Resolve(string? content, string? reasoning, bool hasToolCalls)
    {
        if (hasToolCalls)
        {
            return new ResolvedAnswer(string.Empty, false);
        }
        if (!string.IsNullOrEmpty(content))
        {
            return new ResolvedAnswer(content, false);
        }
        return string.IsNullOrEmpty(reasoning)
            ? new ResolvedAnswer(string.Empty, false)
            : new ResolvedAnswer(reasoning, true);
    }
}
