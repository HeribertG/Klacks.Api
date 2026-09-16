// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The turn-preparation block, moved out of LLMService unchanged (2026-09-16): the pending-confirmation
/// gate, the data-driven recipe resume/match including the ask-step correction, and the previous-action
/// record. Behaviour is identical to the code this replaces - TurnPreparationCharacterizationTests pins
/// that and was green against both.
/// </summary>
/// <param name="pendingConfirmationStore">Outstanding one-time confirmation tokens.</param>
/// <param name="recipeEngine">Data-driven recipe matching and resumption.</param>
/// <param name="recipeRunRecorder">Lifecycle rows of a recipe run (started/aborted/completed).</param>
/// <param name="slotExtractor">One structured model call that pre-fills a fresh recipe's slots.</param>
/// <param name="lastActionStore">Persistence of the previous-action record.</param>
/// <param name="logger">Logger for the recipe lifecycle lines this block already emitted.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.Recipes;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

public class TurnPreparationService : ITurnPreparationService
{
    private readonly IPendingConfirmationStore _pendingConfirmationStore;
    private readonly RecipeEngineService _recipeEngine;
    private readonly IRecipeRunRecorder _recipeRunRecorder;
    private readonly RecipeSlotExtractor _slotExtractor;
    private readonly IAssistantLastActionStore _lastActionStore;
    private readonly ILogger<TurnPreparationService> _logger;

    public TurnPreparationService(
        IPendingConfirmationStore pendingConfirmationStore,
        RecipeEngineService recipeEngine,
        IRecipeRunRecorder recipeRunRecorder,
        RecipeSlotExtractor slotExtractor,
        IAssistantLastActionStore lastActionStore,
        ILogger<TurnPreparationService> logger)
    {
        _pendingConfirmationStore = pendingConfirmationStore;
        _recipeEngine = recipeEngine;
        _recipeRunRecorder = recipeRunRecorder;
        _slotExtractor = slotExtractor;
        _lastActionStore = lastActionStore;
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
    /// </summary>
    internal (bool Force, LLMFunction? ConfirmFunction, string? ContextNote) ResolvePendingConfirmation(LLMContext context)
    {
        if (!AffirmationDetector.IsAffirmation(context.Message)
            || !Guid.TryParse(context.UserId, out var userGuid))
        {
            return (false, null, null);
        }

        var pending = _pendingConfirmationStore.PeekLatestForUser(
            userGuid, TimeSpan.FromSeconds(AutonomyDefaults.ConfirmationForceWindowSeconds));
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
        LLMContext context, string responseContent, IReadOnlyList<LLMFunctionCall> functionCalls, bool recipePaused)
    {
        if (!Guid.TryParse(context.UserId, out var userGuid) || string.IsNullOrEmpty(context.ConversationId))
        {
            return;
        }

        try
        {
            if (recipePaused)
            {
                _lastActionStore.MarkSuperseded(userGuid, context.ConversationId);
                return;
            }

            var executedCalls = functionCalls
                .Where(c => !c.IsRejectedRepeat && !c.RequiresConfirmation)
                .ToList();

            if (executedCalls.Count == 0)
            {
                _lastActionStore.MarkSuperseded(userGuid, context.ConversationId);
                return;
            }

            _lastActionStore.Save(new AssistantLastAction
            {
                UserId = userGuid,
                ConversationId = context.ConversationId,
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
        if (function == null || string.IsNullOrWhiteSpace(function.Description))
        {
            return null;
        }

        var sentenceEnd = function.Description.IndexOf('.');
        var label = (sentenceEnd > 0 ? function.Description[..sentenceEnd] : function.Description).Trim();

        return label.Length <= GracefulCorrectionDefaults.OptionLabelMaxLength
            ? label
            : label[..GracefulCorrectionDefaults.OptionLabelMaxLength].TrimEnd();
    }
}