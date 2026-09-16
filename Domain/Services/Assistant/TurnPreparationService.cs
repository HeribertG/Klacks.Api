// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Everything both chat entry points have to settle BEFORE the model is called, and the one thing they
/// record after it. Five responsibilities, in the order a turn meets them:
/// (1) the pending-confirmation gate - an affirmation that redeems an outstanding one-time token narrows
///     this turn to confirm_pending_action and resurfaces the token the history does not carry;
/// (2) the data-driven recipe resume or fresh match, including the ask-step's own correction, cancel and
///     topic-switch branches;
/// (3) correction planning (PlanCorrectionAsync) - gates G0-G5, ending in the composite this turn routes
///     on and the skills it excludes;
/// (4) correction completion (CompleteCorrection) - resolves the inverse call of the corrected turn and
///     hands it to CorrectionOutcomeComposer, which is pure and therefore testable without this class's
///     dependencies; the resolution lives here because it needs one of them;
/// (5) the previous-action record (RecordLastAction), the anchor a later correction reads.
/// Extracted from LLMService in 2026-09; TurnPreparationCharacterizationTests pinned that move and was
/// green against both sides of it, but the class has grown behaviour since and is no longer a copy.
/// </summary>
/// <param name="pendingConfirmationStore">Outstanding one-time confirmation tokens.</param>
/// <param name="recipeEngine">Data-driven recipe matching and resumption.</param>
/// <param name="recipeRunRecorder">Lifecycle rows of a recipe run (started/aborted/completed).</param>
/// <param name="slotExtractor">One structured model call that pre-fills a fresh recipe's slots.</param>
/// <param name="lastActionStore">Persistence of the previous-action record.</param>
/// <param name="routeProbe">Gate G5 of the correction path: does the correction route on its own?</param>
/// <param name="inverseResolver">The inverse call of a write the corrected turn made, when one exists.</param>
/// <param name="logger">Logger for the recipe lifecycle lines this block already emitted.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

public class TurnPreparationService : ITurnPreparationService
{
    private readonly IPendingConfirmationStore _pendingConfirmationStore;
    private readonly RecipeEngineService _recipeEngine;
    private readonly IRecipeRunRecorder _recipeRunRecorder;
    private readonly RecipeSlotExtractor _slotExtractor;
    private readonly IAssistantLastActionStore _lastActionStore;
    private readonly IDeterministicRouteProbe _routeProbe;
    private readonly ISkillInverseResolver _inverseResolver;
    private readonly ILogger<TurnPreparationService> _logger;

    public TurnPreparationService(
        IPendingConfirmationStore pendingConfirmationStore,
        RecipeEngineService recipeEngine,
        IRecipeRunRecorder recipeRunRecorder,
        RecipeSlotExtractor slotExtractor,
        IAssistantLastActionStore lastActionStore,
        IDeterministicRouteProbe routeProbe,
        ISkillInverseResolver inverseResolver,
        ILogger<TurnPreparationService> logger)
    {
        _pendingConfirmationStore = pendingConfirmationStore;
        _recipeEngine = recipeEngine;
        _recipeRunRecorder = recipeRunRecorder;
        _slotExtractor = slotExtractor;
        _lastActionStore = lastActionStore;
        _routeProbe = routeProbe;
        _inverseResolver = inverseResolver;
        _logger = logger;
    }

    public async Task<TurnPreparation> PrepareAsync(
        TurnPreparationRequest request, CancellationToken cancellationToken = default)
    {
        var (force, confirmFunction, note) = ResolvePendingConfirmation(request.Context);

        var plan = await ResolveOrResumeRecipeAsync(
            request.Context, request.Provider, request.Model, request.ConversationId, cancellationToken);

        return new TurnPreparation(plan, force, confirmFunction, note);
    }

    // (a) Moved verbatim from LLMService.cs:133-174, including its summary comment.
    /// <summary>
    /// Decides whether the current turn should be forced to confirm an outstanding pending action.
    /// Fires only when the user message is a clear affirmation AND the user still has an un-consumed
    /// confirmation in the store AND confirm_pending_action is in scope. Returns the (always-on)
    /// confirm function to narrow the tool scope to, plus a context note that resurfaces the token
    /// (which is lost from conversation history because only user/assistant text is persisted).
    /// A pending gate-replay row deliberately overrides a mutation intent in the same message: a reply
    /// that restates the action ("yes, delete the user") is still an answer to the question the gate
    /// asked. Vetoing it here made the model re-call the skill, which produced a fresh hold and a
    /// confirmation loop. Only the first iteration is narrowed, so any additional request in the same
    /// message is still served once the token is redeemed.
    /// The correction undo of rule 3 is redeemed through the same seam but expires differently: the offer
    /// is made once, inside one answer and never as a separate dialogue, so its token belongs to the turn
    /// that immediately follows it. Any message that does not affirm therefore DISCARDS it - without that,
    /// a "ja" to whatever the model asked next would redeem the undo instead and carry out a
    /// gate-bypassing write the user never confirmed. It is read before the gate-replay row because it is
    /// the more recent offer by construction, and reading it first also guarantees no undo row survives an
    /// affirming turn: if one existed, it won.
    /// The turn that MAKES the offer is excluded from both halves. The entry points write that token
    /// immediately before the model call this method runs inside, so the offering turn would otherwise
    /// see its own row: it would discard it (the correction message is not an affirmation, and the offer
    /// would be dead before the user ever read it) or, for a correction that opens with "ja, ich meinte
    /// ...", redeem it and carry the undo out before it was offered at all. GracefulCorrectionApplied is
    /// already on the context by then and says exactly that. The residue: a correction that follows
    /// another correction leaves the older row untouched for one more turn. When it makes an offer of its
    /// own, Create drops the predecessor and nothing is left over; when it makes none, the older row stays
    /// redeemable until the force window closes. That is the smaller evil - the alternative kills every
    /// fresh offer on the turn that makes it.
    /// </summary>
    internal (bool Force, LLMFunction? ConfirmFunction, string? ContextNote) ResolvePendingConfirmation(LLMContext context)
    {
        if (!Guid.TryParse(context.UserId, out var userGuid))
        {
            return (false, null, null);
        }

        var undoIsOfferedThisTurn = context.GracefulCorrectionApplied;

        if (!AffirmationDetector.IsAffirmation(context.Message))
        {
            if (!undoIsOfferedThisTurn)
            {
                _pendingConfirmationStore.DiscardCorrectionUndo(userGuid);
            }

            return (false, null, null);
        }

        var forceWindow = TimeSpan.FromSeconds(AutonomyDefaults.ConfirmationForceWindowSeconds);
        var pending = undoIsOfferedThisTurn
            ? null
            : _pendingConfirmationStore.PeekLatestForUser(
                userGuid, forceWindow, PendingConfirmationPurposes.CorrectionUndo);

        pending ??= _pendingConfirmationStore.PeekLatestForUser(
            userGuid, forceWindow, PendingConfirmationPurposes.GateReplay);
        if (pending == null)
        {
            return (false, null, null);
        }

        var confirmFunction = context.AvailableFunctions.FirstOrDefault(
            f => string.Equals(f.Name, AutonomyDefaults.ConfirmPendingActionSkillName, StringComparison.OrdinalIgnoreCase));
        if (confirmFunction == null)
        {
            return (false, null, null);
        }

        var note = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            MutationGuardConstants.PendingConfirmationContextTemplate,
            pending.SkillName,
            pending.Token);

        return (true, confirmFunction, note);
    }

    // (b) Moved verbatim from LLMService.cs:1373-1483, including its comment block.
    // Data-driven recipe engine entry point (shared by both loops): resume a recipe paused on an ask by
    // raw-filling the current ask slot from the user's message, otherwise match a fresh recipe and
    // pre-fill its slots from the opening message via one structured extraction call. In both cases
    // advance past any already-satisfied steps so the loop sees the next ask (pause) or push (force).
    // A recipe paused on the confirmation gate (semantic match) is a third resume shape: an affirmation
    // clears the gate and proceeds, anything else (rejection, off-topic reply, a question) discards the
    // pending recipe and falls through to a fresh match on the current message instead.
    internal async Task<RecipeExecutionPlan?> ResolveOrResumeRecipeAsync(
        LLMContext context,
        ILLMProvider provider,
        LLMModel model,
        string conversationId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(context.UserId, out var userGuid))
        {
            return null;
        }

        var resumed = await _recipeEngine.ResumeAsync(userGuid, conversationId, cancellationToken);
        if (resumed != null)
        {
            if (resumed.NeedsConfirmation)
            {
                if (!AffirmationDetector.IsAffirmation(context.Message))
                {
                    await _recipeRunRecorder.AbortRunningAsync(
                        resumed.Name, userGuid, conversationId, "confirmation declined", cancellationToken);
                    _recipeEngine.Clear(userGuid, conversationId);
                    resumed = null;
                }
                else
                {
                    resumed.ConfirmAndProceed();
                    resumed.AdvanceOverSatisfied();
                    return resumed;
                }
            }
            else
            {
                var step = resumed.CurrentStep;
                if (resumed.CurrentIsAsk && !string.IsNullOrWhiteSpace(step?.Slot))
                {
                    // An explicit abort ("abbrechen", "vergiss es", "cancel") must end the recipe, not be
                    // raw-filled into the slot as if it were the answer to the ask question.
                    if (RecipeCancellationDetector.IsCancellation(context.Message))
                    {
                        await _recipeRunRecorder.AbortRunningAsync(
                            resumed.Name, userGuid, conversationId, "cancelled during ask step", cancellationToken);
                        _recipeEngine.Clear(userGuid, conversationId);
                        _logger.LogInformation(
                            "Recipe '{Recipe}' cancelled by user during ask step (slot {Slot})", resumed.Name, step!.Slot);
                        return null;
                    }

                    // An independent question ("Wie kann ich die XML einbinden?") is not an answer to the
                    // pending slot either — raw-filling it would silence every skill the tool-less ask-step
                    // call could otherwise have used to answer it. Leave the slot unfilled and let the loop
                    // run this one turn with its full toolset instead; the recipe stays on this same ask
                    // step and is re-asked once that turn's own answer is done.
                    if (RecipeTopicSwitchDetector.IsTopicSwitch(context.Message))
                    {
                        resumed.MarkTopicSwitchThisTurn();
                        _logger.LogInformation(
                            "Recipe '{Recipe}' ask step (slot {Slot}) bypassed for one turn: message reads " +
                            "as an independent question, running a normal full-tool turn and re-asking afterwards",
                            resumed.Name, step!.Slot);
                    }
                    // Checked after the topic switch, not before it. A topic switch requires a question
                    // mark, an interrogative lead and two words, so it is the more specific finding — and
                    // the recoverable one, since it answers the question and re-asks the same slot on the
                    // next turn. A message satisfying both ("Nein, nicht so — wie finde ich heraus, welche
                    // Gruppen ein Mitarbeiter schon hat?") is far more likely an independent question, and
                    // aborting for it would trade a recoverable turn for a lost recipe. The correction this
                    // branch exists for carries no question mark, so the ordering costs it nothing.
                    else if (RecipeCorrectionDetector.IsStrongCorrection(context.Message, resumed))
                    {
                        await _recipeRunRecorder.AbortRunningAsync(
                            resumed.Name, userGuid, conversationId, RecipeAbortReasons.CorrectedDuringAskStep, cancellationToken);
                        _recipeEngine.Clear(userGuid, conversationId);
                        _logger.LogInformation(
                            "Recipe '{Recipe}' aborted during ask step (slot {Slot}): message reads as a " +
                            "correction of the recipe", resumed.Name, step!.Slot);

                        return await ResolveAfterCorrectionAsync(
                            resumed.Name, resumed.TriggerMessage, context, provider, model, cancellationToken);
                    }
                    else
                    {
                        resumed.FillSlot(step!.Slot!, context.Message);
                    }
                }

                resumed.AdvanceOverSatisfied();
                return resumed;
            }
        }

        var fresh = await _recipeEngine.ResolveAsync(context.Message, context.Language, context.UserRights, cancellationToken);
        if (fresh != null)
        {
            var extracted = await _slotExtractor.ExtractAsync(
                provider, model, context.Message, fresh.AskSlotHints(), cancellationToken);
            fresh.PrefillSlots(extracted);
            fresh.AdvanceOverSatisfied();
            _logger.LogInformation(
                "Recipe '{Recipe}' engaged: prefilled slots [{Slots}]", fresh.Name, string.Join(", ", fresh.Slots.Keys));
        }

        return fresh;
    }

    // (b) Moved verbatim from LLMService.cs:1485-1531, including its summary comment.
    /// <summary>
    /// Re-engages a recipe after the pending one was aborted because the user corrected it.
    ///
    /// Only a plan that stops on an ask is handed back. This turn's toolset was assembled before
    /// LLMService ran and guaranteed the step skills of the recipe that was just aborted, so a plan whose
    /// first open step forces a skill cannot be driven: ResolveRecipeIteration finds the skill missing and
    /// the forcing silently no-ops, leaving an active plan that never pauses on an ask and is therefore
    /// never persisted. An ask step needs no tool at all, so it is the one shape this turn can still
    /// execute. Re-engaging a push-first recipe belongs to the assembler, which is where the composite
    /// intent message has to be built anyway.
    ///
    /// A fresh plan for the recipe that was just aborted cannot occur: it is excluded from the match
    /// rather than discarded afterwards, because re-matching it would restart at step 0 with every slot
    /// the user already supplied thrown away, which is worse than the raw-fill this branch prevented.
    /// </summary>
    private async Task<RecipeExecutionPlan?> ResolveAfterCorrectionAsync(
        string abortedRecipeName,
        string? triggerMessage,
        LLMContext context,
        ILLMProvider provider,
        LLMModel model,
        CancellationToken cancellationToken)
    {
        // The composite, not the correction. On its own the correction usually resolves to nothing: it
        // opens with a negation and carries no mutation verb, so the engine suppresses the semantic
        // fallback - correctly, because such a message is not a standalone request. The intent sits in the
        // message that triggered the recipe, which PendingRecipe.TriggerMessage now carries across turns.
        // Composed through RecipeCorrectionComposer because the toolset assembler produces the same bytes:
        // FindMatchingRecipeAsync memoizes on (message, language, excluded), so identical composition and
        // identical exclusion mean this call reuses the entry GuaranteedSkillNamesAsync already warmed for
        // this turn instead of paying for a second embedding round.
        var composite = RecipeCorrectionComposer.Compose(triggerMessage, context.Message);

        var fresh = await _recipeEngine.ResolveAsync(
            composite, context.Language, context.UserRights, cancellationToken, abortedRecipeName);
        if (fresh == null)
        {
            return null;
        }

        var extracted = await _slotExtractor.ExtractAsync(
            provider, model, composite, fresh.AskSlotHints(), cancellationToken);
        fresh.PrefillSlots(extracted);
        fresh.AdvanceOverSatisfied();

        return fresh.CurrentIsAsk ? fresh : null;
    }

    public void RecordLastAction(
        LLMContext context,
        string conversationId,
        string responseContent,
        IReadOnlyList<LLMFunctionCall> functionCalls,
        bool recipePaused)
    {
        if (!Guid.TryParse(context.UserId, out var userGuid) || string.IsNullOrEmpty(conversationId))
        {
            return;
        }

        try
        {
            if (recipePaused)
            {
                _lastActionStore.MarkSuperseded(userGuid, conversationId);
                return;
            }

            var executedCalls = functionCalls
                .Where(c => !c.IsRejectedRepeat && !c.RequiresConfirmation)
                .ToList();

            if (executedCalls.Count == 0)
            {
                _lastActionStore.MarkSuperseded(userGuid, conversationId);
                return;
            }

            _lastActionStore.Save(new AssistantLastAction
            {
                UserId = userGuid,
                ConversationId = conversationId,
                UserMessage = context.Message,
                AssistantAnswerExcerpt = responseContent,
                CreateTimeUtc = DateTime.UtcNow,
                Calls = executedCalls.Select(call => ToLastActionCall(context, call)).ToList()
            });
        }
        catch (Exception ex)
        {
            // A missing anchor costs one correction; a thrown store call would cost the answer the user
            // is waiting for. The write is best-effort by design, the read is gated on CanAnchorCorrection.
            _logger.LogWarning(ex, "Could not record the previous action for user {UserId}", context.UserId);
        }
    }

    /// <summary>
    /// Gates G0-G4 are evaluated first and only then is the (comparatively expensive) G5 probe paid for,
    /// so a message that was going to be rejected anyway never runs it.
    ///
    /// A turn without a resolved agent fails closed before any gate runs: the G5 probe would then have no
    /// skills to guarantee and would answer "does not route alone" for every message, which OPENS the
    /// correction path on exactly the turns that already lost their toolset.
    ///
    /// A broken probe is treated as "the correction routes alone", i.e. NO correction. The probe throws
    /// rather than returning an empty list precisely because empty means "does not route alone" and would
    /// OPEN the correction path: swallowing the failure into an empty result would bias every probe
    /// outage towards re-routing a request the user never made. Rejecting instead costs at most one
    /// missed repair, which is the precision-biased half of that trade.
    /// </summary>
    public async Task<GracefulCorrectionPlan?> PlanCorrectionAsync(
        GracefulCorrectionInput input, CancellationToken cancellationToken = default)
    {
        if (input.Agent == null)
        {
            _logger.LogDebug("Graceful correction rejected: no agent was resolved for this turn.");
            return null;
        }

        var gate = GracefulCorrectionDetector.Evaluate(
            input.Message, input.LastAction, input.RecipeIsActive,
            correctionRoutesAlone: false, DateTime.UtcNow);

        if (gate != GracefulCorrectionGate.Passed)
        {
            _logger.LogDebug("Graceful correction rejected by gate {Gate}", gate);
            return null;
        }

        bool routesAlone;
        try
        {
            routesAlone = (await _routeProbe.GuaranteedSkillNamesAsync(
                input.Agent, input.UserRights, input.Message, input.Language, cancellationToken)).Count > 0;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex,
                "The deterministic route probe failed for user {UserId}; treating the message as a " +
                "self-contained request, so this turn runs without a correction.", input.UserId);
            routesAlone = true;
        }

        if (routesAlone)
        {
            _logger.LogDebug("Graceful correction rejected by gate {Gate}", GracefulCorrectionGate.RoutesAlone);
            return null;
        }

        var lastAction = input.LastAction!;
        var excluded = GracefulCorrectionDetector.ExcludedSkillNames(lastAction);

        _logger.LogInformation(
            "Graceful correction engaged: re-routing on the composite of the previous request and the " +
            "correction; excluding {Skills}", string.Join(", ", excluded));

        return new GracefulCorrectionPlan(
            lastAction,
            input.Message,
            RecipeCorrectionComposer.Compose(lastAction.UserMessage, input.Message),
            excluded);
    }

    public GracefulCorrectionOutcome CompleteCorrection(
        GracefulCorrectionPlan plan,
        IReadOnlyList<LLMFunction> assembledFunctions,
        string? language)
    {
        var (undo, undoneCall) = ResolveUndo(plan);
        var outcome = CorrectionOutcomeComposer.Compose(plan, assembledFunctions, language, undo, undoneCall);

        if (outcome.Undo != null && undoneCall != null)
        {
            _logger.LogInformation(
                "Graceful correction: offering to undo '{Skill}' with '{Inverse}'",
                undoneCall.SkillName, outcome.Undo.SkillName);
        }

        return outcome;
    }

    /// <summary>
    /// At most one undo per correction, for the FIRST reversible write the corrected turn made. Not one
    /// per call: rule 3 asks for a single yes/no sentence, and a turn that wrote twice would otherwise
    /// produce a menu. The matched call is returned alongside the invocation rather than assumed to be
    /// Calls[0] - a turn that looked something up before it wrote has the reversible call in a later
    /// position, and naming the read in the undo sentence would say the assistant is about to undo the
    /// search. Nothing is persisted here: the caller decides whether a confirmation token is written,
    /// which is what keeps the headless replay side-effect-free. Whether the offer is made at all is
    /// decided by the composer, which drops it next to a clarification.
    /// </summary>
    /// <param name="plan">The correction the planning decided on, with the previous action it anchors to</param>
    private (SkillUndoInvocation? Undo, AssistantLastActionCall? UndoneCall) ResolveUndo(GracefulCorrectionPlan plan)
    {
        foreach (var call in plan.LastAction.Calls)
        {
            if (_inverseResolver.TryResolve(call, out var undo) && undo != null)
            {
                return (undo, call);
            }
        }

        return (null, null);
    }

    private static AssistantLastActionCall ToLastActionCall(LLMContext context, LLMFunctionCall call) => new()
    {
        SkillName = call.FunctionName,
        SkillDisplayLabel = DescribeCalledSkill(context, call.FunctionName),
        ArgumentsJson = System.Text.Json.JsonSerializer.Serialize(call.Parameters),
        ResultDataJson = call.DataJson.Count > 0 ? call.DataJson[0] : GracefulCorrectionDefaults.EmptyJsonObject,
        IsReadOnly = ReadOnlySkillPrefixes.HasReadOnlyPrefix(call.FunctionName),
        Success = call.Success
    };

    /// <summary>
    /// The user-facing label of a skill, taken from THIS turn's toolset - the last moment it is
    /// available. The next turn excludes that skill, so a lookup there returns nothing and the note
    /// would have to fall back to the internal name, which must never reach a user.
    /// </summary>
    private static string? DescribeCalledSkill(LLMContext context, string functionName)
    {
        var function = context.AvailableFunctions.FirstOrDefault(
            f => string.Equals(f.Name, functionName, StringComparison.OrdinalIgnoreCase));

        return CorrectionOutcomeComposer.FirstSentenceLabel(
            function?.Description, GracefulCorrectionDefaults.SkillDisplayLabelMaxLength);
    }
}
