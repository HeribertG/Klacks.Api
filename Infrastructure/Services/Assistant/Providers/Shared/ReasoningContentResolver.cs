// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

/// <summary>
/// Decides what an OpenAI-compatible reasoning model's effective answer is, given the regular
/// content, the reasoning_content channel, and whether the turn produced tool calls. The reasoning
/// channel is chain-of-thought and is NEVER the answer: until 2026-09-24 it was returned as the answer
/// when content was empty, and users saw the model's deliberation (e.g. about a tool it did not have)
/// as Klacksy's reply. An empty answer is left to the callers, which already treat it as "no answer"
/// (chat: EmptyAnswerRecovery, recipe steps: RecipeReplyGuard, greeting: template fallback).
/// </summary>
public static class ReasoningContentResolver
{
    /// <summary>
    /// Resolves the answer AND reports whether the model reasoned without writing any content, so the
    /// provider can log that case; the flag never changes what is shown.
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
        return new ResolvedAnswer(string.Empty, !string.IsNullOrEmpty(reasoning));
    }
}
