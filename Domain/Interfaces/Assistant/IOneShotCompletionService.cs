// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Runs exactly one tool-free LLM completion (system prompt + one user message, temperature 0) directly
/// against the provider — deliberately WITHOUT the Klacksy chat pipeline: no conversation or history,
/// no soul/memory prompt, no recipe engine, no tools, no usage/last-action recording, no background
/// tasks (auto-memory, learning cases) and no persistence. Meant for background classification or
/// extraction over foreign text (inbound email/messenger, spam filter), where the chat pipeline would
/// both distort the reply and write foreign content into a user's history and memory.
/// Never throws for provider failures; cancellation is propagated.
/// </summary>
public interface IOneShotCompletionService
{
    /// <summary>
    /// Sends one completion request, retrying transient provider errors.
    /// </summary>
    /// <param name="systemPrompt">All instructions for the model</param>
    /// <param name="userMessage">The data to process (e.g. the inbound message)</param>
    /// <param name="modelId">Klacks model id to use; null resolves the configured default model</param>
    /// <param name="cancellationToken">Cancels the provider call and the retry backoff</param>
    Task<OneShotCompletionResult> CompleteAsync(
        string systemPrompt,
        string userMessage,
        string? modelId = null,
        CancellationToken cancellationToken = default);
}
