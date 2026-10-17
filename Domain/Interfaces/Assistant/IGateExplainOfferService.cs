// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The "explain briefly first" offer at the gates of consequential actions. Two seams around one chat turn:
/// PrepareTurnAsync runs before the model and settles a pending offer from the user's message (accepted: the turn is
/// grounded in the topic's curated content; anything else: Dismissed, with the outcome proceeded/declined/other/expired);
/// AugmentRepliesAsync runs after the model and adds the chip when the turn ended at a gate, or - on the turn that followed
/// an accepted offer - renews exactly the held confirmation the offer was made at and makes sure the user can answer yes
/// or no again. Best effort by contract: a failure never reaches the chat turn.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IGateExplainOfferService
{
    /// <summary>
    /// Settles a pending offer of the conversation against the user's message. On acceptance sets
    /// LLMContext.ExplainOfferNote and LLMContext.ExplainOfferAcceptance; on any other message records the offer as
    /// dismissed together with its outcome.
    /// </summary>
    /// <param name="context">The turn context, carrying user, conversation, language, message and explanation depth</param>
    /// <param name="cancellationToken">Cancels the lookups</param>
    Task PrepareTurnAsync(LLMContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// The reply options the turn should end with: the model's own options plus the explain chip when the turn ended at a gate
    /// and an offer is allowed, or unchanged otherwise.
    /// </summary>
    /// <param name="context">The turn context</param>
    /// <param name="conversationId">Conversation the turn belongs to; the offer is remembered under it</param>
    /// <param name="functionCalls">The typed calls the turn executed (LLMResponse/SseChunk.ExecutedCalls), not the wire projection</param>
    /// <param name="replies">The reply options the model produced, null when it produced none; never modified in place</param>
    /// <param name="cancellationToken">Cancels the lookups</param>
    Task<SuggestedRepliesConfig?> AugmentRepliesAsync(
        LLMContext context,
        string? conversationId,
        IReadOnlyList<LLMFunctionCall>? functionCalls,
        SuggestedRepliesConfig? replies,
        CancellationToken cancellationToken = default);
}
