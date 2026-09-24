// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Logging of the reasoning_content channel for the OpenAI-compatible providers. The reasoning text is
/// chain-of-thought and may quote user data, so it is written at Debug level only; a call whose model
/// reasoned but wrote no content and no tool call is reported at Warning level with its finish_reason,
/// because the caller then receives an empty answer and has to recover it.
/// </summary>

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

public static class ReasoningChannelLog
{
    private const int MaxLoggedReasoningLength = 1000;
    private const string TruncationSuffix = "...";

    /// <summary>
    /// Writes the reasoning text at Debug level and warns when the call ended with reasoning only.
    /// </summary>
    /// <param name="logger">The provider's logger</param>
    /// <param name="providerName">Display name of the provider, for the log line</param>
    /// <param name="modelId">The model the call went to</param>
    /// <param name="reasoning">The complete reasoning_content of the call (may be empty)</param>
    /// <param name="reasoningWithoutContent">True when the call produced reasoning but neither content nor a tool call</param>
    /// <param name="finishReason">The finish_reason reported by the API, when it sent one</param>
    public static void Write(
        ILogger logger, string providerName, string? modelId, string? reasoning,
        bool reasoningWithoutContent, string? finishReason)
    {
        if (!string.IsNullOrEmpty(reasoning) && logger.IsEnabled(LogLevel.Debug))
        {
            var logged = reasoning.Length <= MaxLoggedReasoningLength
                ? reasoning
                : reasoning[..MaxLoggedReasoningLength] + TruncationSuffix;
            logger.LogDebug("{Provider} reasoning_content ({Length} chars): {Reasoning}", providerName, reasoning.Length, logged);
        }

        if (reasoningWithoutContent)
        {
            logger.LogWarning(
                "{Provider} model {Model} wrote {Length} chars of reasoning but no content and no tool call "
                + "(finish_reason={FinishReason}); the answer stays empty, reasoning is never shown",
                providerName, modelId, reasoning?.Length ?? 0, finishReason);
        }
    }
}
