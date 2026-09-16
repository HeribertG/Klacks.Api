// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Everything a chat turn decides before its first provider call, and the one thing it records after
/// its last: the outstanding-confirmation gate, the data-driven recipe engine's resume/match, and the
/// previous-action record the next turn's correction path reads. Extracted from LLMService, which had
/// grown to 1760 lines around this block; both ProcessAsync and ProcessStreamAsync go through the same
/// PrepareAsync call so the two paths cannot diverge.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ITurnPreparationService
{
    Task<TurnPreparation> PrepareAsync(TurnPreparationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores the anchor the next turn's correction path reads. Called synchronously at the end of the
    /// turn, before the fire-and-forget background tasks: the trajectory can land after the user's next
    /// message has already been routed and therefore cannot serve as the anchor. A turn without an
    /// executed call marks the existing record superseded instead of replacing it, and so does a turn
    /// that left a recipe paused on an ask - the user's next message answers the recipe question and is
    /// never a correction.
    ///
    /// The conversation id is passed in rather than read off the context: the context carries only what
    /// the client sent, which is null on the first turn of a new conversation, while the chat loop has
    /// already resolved the persisted conversation by the time it records. Keying the anchor by the
    /// resolved id makes it the same key the pending-recipe store uses, so the two per-conversation
    /// records of a turn cannot end up under different keys.
    ///
    /// That id is length-validated only at the HTTP boundary (LLMRequest.ConversationId carries the
    /// [StringLength] attribute). A non-HTTP caller must supply ids within
    /// GracefulCorrectionDefaults.ConversationIdMaxLength itself; the store deliberately does not cap
    /// the key it later queries with.
    /// </summary>
    /// <param name="context">The turn's context, source of the user id, the user message and this turn's toolset.</param>
    /// <param name="conversationId">The resolved conversation id the record is keyed by; a null or empty value records nothing.</param>
    /// <param name="responseContent">The assistant's answer, stored as the excerpt the correction note quotes back.</param>
    /// <param name="functionCalls">Every call of the turn, including the rejected and held ones this filters out.</param>
    /// <param name="recipePaused">True when the turn left a recipe waiting on an ask or confirmation step.</param>
    void RecordLastAction(
        LLMContext context,
        string conversationId,
        string responseContent,
        IReadOnlyList<LLMFunctionCall> functionCalls,
        bool recipePaused);
}
