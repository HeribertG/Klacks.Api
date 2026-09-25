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

    /// <summary>
    /// True when the conversation's previous-action record was written or superseded after the given time,
    /// which is how a turn persisted late (stopped, cut off) learns that a newer turn has already moved on and
    /// must not overwrite that turn's anchor. A failing store reads as "no newer record".
    /// </summary>
    /// <param name="context">The turn's context, source of the user id.</param>
    /// <param name="conversationId">The resolved conversation id the record is keyed by.</param>
    /// <param name="sinceUtc">The start of the turn that is about to record; anything after it is newer.</param>
    bool HasLastActionSince(LLMContext context, string conversationId, DateTime sinceUtc);

    /// <summary>
    /// Part (c) of the turn preparation: decides whether this turn corrects the previous one and what
    /// the re-routing must run on. Called BEFORE the toolset assembly, because the composite is the
    /// assembler's input and the exclusion is one of its parameters.
    ///
    /// The caller must await this to completion before it starts the assembly and must never run the
    /// two concurrently: the gate G5 probe and the assembler share the scoped RecipeEngineService and
    /// its non-atomic match memo, so a parallel start races two writers onto one tuple.
    /// </summary>
    /// <param name="input">Message, rights, language plus the previous action and the active-recipe flag the caller read from its stores.</param>
    /// <param name="cancellationToken">Cancellation of the turn.</param>
    Task<GracefulCorrectionPlan?> PlanCorrectionAsync(
        GracefulCorrectionInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// The second half, called AFTER the assembly: reads the deterministic candidates out of the
    /// assembled toolset and produces the volatile note and the clarification question when there is no
    /// clear winner. Creates nothing and persists nothing - the caller decides whether a confirmation
    /// token is written, so a headless replay stays side-effect-free.
    /// </summary>
    /// <param name="plan">The plan PlanCorrectionAsync returned for this turn.</param>
    /// <param name="assembledFunctions">This turn's final toolset, the source of the deterministic candidates.</param>
    /// <param name="language">The single language tag the whole answer must be written in.</param>
    /// <param name="undoIsPermitted">
    /// False suppresses the undo half of the outcome - no offer sentence in the note and no invocation
    /// for the caller to hold. The decision belongs to the caller because it needs the account behind
    /// the turn, which this service does not read; a caller that persists nothing (the headless replay)
    /// leaves it at true and keeps resolving the undo as data.
    /// </param>
    GracefulCorrectionOutcome CompleteCorrection(
        GracefulCorrectionPlan plan,
        IReadOnlyList<LLMFunction> assembledFunctions,
        string? language,
        bool undoIsPermitted = true);

    /// <summary>
    /// The inverse invocation CompleteCorrection would offer for this plan, or null when there is none.
    /// Exists so a caller can answer "may this account release that skill" BEFORE the note is composed:
    /// an offer the redemption would refuse must leave neither a sentence nor a token behind, and by the
    /// time the outcome exists the sentence is already in it. Resolves the same way CompleteCorrection
    /// does, reads nothing and persists nothing.
    /// </summary>
    /// <param name="plan">The plan PlanCorrectionAsync returned for this turn.</param>
    SkillUndoInvocation? PeekUndo(GracefulCorrectionPlan plan);
}
