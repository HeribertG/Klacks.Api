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
/// <param name="routeProbe">Gate G5 of the correction path: does the correction route on its own?</param>
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
    private readonly ILogger<TurnPreparationService> _logger;

    public TurnPreparationService(
        IPendingConfirmationStore pendingConfirmationStore,
        RecipeEngineService recipeEngine,
        IRecipeRunRecorder recipeRunRecorder,
        RecipeSlotExtractor slotExtractor,
        IAssistantLastActionStore lastActionStore,
        IDeterministicRouteProbe routeProbe,
        ILogger<TurnPreparationService> logger)
    {
        _pendingConfirmationStore = pendingConfirmationStore;
        _recipeEngine = recipeEngine;
        _recipeRunRecorder = recipeRunRecorder;
        _slotExtractor = slotExtractor;
        _lastActionStore = lastActionStore;
        _routeProbe = routeProbe;
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
        var correctedCall = plan.LastAction.Calls.FirstOrDefault();
        var previousLabel = LabelOf(correctedCall);
        var previousArguments = correctedCall?.ArgumentsJson ?? GracefulCorrectionDefaults.EmptyJsonObject;

        var candidates = DeterministicCandidates(assembledFunctions)
            .OrderByDescending(f => f.RetrievalScore ?? 0.0)
            .ToList();

        var clarification = BuildClarification(candidates, previousLabel, language);

        var openingSentence = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            GracefulCorrectionNotes.OpeningSentenceTemplate,
            previousLabel);

        var note = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            GracefulCorrectionNotes.CorrectionContextTemplate,
            previousLabel,
            previousArguments,
            CapCorrection(plan.CorrectionMessage),
            openingSentence,
            AnswerLanguage(language));

        if (candidates.Count == 0)
        {
            note += GracefulCorrectionNotes.NoCandidateSuffix;
        }

        return clarification == null
            ? new GracefulCorrectionOutcome(note, null, [])
            : new GracefulCorrectionOutcome(
                note,
                clarification,
                candidates
                    .Take(GracefulCorrectionDefaults.ClarificationCandidateCount)
                    .Select(candidate => candidate.Name)
                    .ToList());
    }

    /// <summary>
    /// A label for what a recorded call did. Never the internal snake_case name: the label was captured
    /// from the toolset of the turn that made the call (AssistantLastActionCall), because by the time the
    /// correction turn runs that skill is excluded from the toolset and cannot be looked up any more.
    /// Without a label the English stand-in of the note is used rather than the user-facing German
    /// redaction, because this text is substituted into a model-facing instruction.
    /// </summary>
    private static string LabelOf(AssistantLastActionCall? call) =>
        string.IsNullOrWhiteSpace(call?.SkillDisplayLabel)
            ? GracefulCorrectionNotes.UnnamedPreviousActionLabel
            : call!.SkillDisplayLabel!;

    /// <summary>
    /// The correction as the note quotes it. The message is LIVE user input and, unlike the anchor's own
    /// fields, was never capped by the store, so an over-long paste would otherwise push the note past
    /// the history budget it is itself measured against. Capped to the same length the anchor's user
    /// message is stored at, by a hard slice for the reason RecipeCorrectionComposer.CapForStorage gives.
    /// </summary>
    private static string CapCorrection(string correction) =>
        correction.Length <= GracefulCorrectionDefaults.UserMessageMaxLength
            ? correction
            : correction[..GracefulCorrectionDefaults.UserMessageMaxLength];

    /// <summary>
    /// The deterministically guaranteed skills of the composite: keyword/synonym matches and recipe step
    /// skills. Retrieved and expanded skills are excluded on purpose - they are a ranking, and a ranking
    /// is exactly what a correction cannot be trusted to have got right. Always-on skills carry
    /// ToolsetSkillSource.AlwaysOn and are outside this set already; confirm_pending_action is removed by
    /// name because it is always-on for a different reason and is never an intent.
    /// </summary>
    internal static List<LLMFunction> DeterministicCandidates(IReadOnlyList<LLMFunction> functions) =>
        functions
            .Where(f => f.ToolsetSource is ToolsetSkillSource.Keyword or ToolsetSkillSource.RecipeStep)
            .Where(f => !string.Equals(
                f.Name, AutonomyDefaults.ConfirmPendingActionSkillName, StringComparison.OrdinalIgnoreCase))
            .ToList();

    /// <summary>
    /// The two-option question of design rule 2, or null when the turn may act.
    ///
    /// The rule, explicitly, because it is the one judgement call of this feature:
    ///   - fewer than two candidates    -> no question, because a question offers exactly two options;
    ///   - both candidates carry a retrieval score and the gap is at most
    ///     CorrectionAmbiguityTolerance -> ask, the ranking does not separate them;
    ///   - neither carries a score      -> ask, nothing ranks them at all (a keyword guarantee is a
    ///                                     yes/no, not a degree, so two of them are simply tied);
    ///   - exactly one carries a score  -> act on that one, because retrieval judged it relevant while
    ///                                     the other is only a literal keyword hit;
    ///   - both scored, gap larger      -> act on the better one.
    /// A null score is therefore NOT read as zero. Treating it as zero made every pair of keyword
    /// guarantees tie with every unscored recipe step, which asked far more often than rule 2 intends.
    /// The two boundary cases are measured by correction-v1 (cr-de-005-ambiguous, cr-de-007-clear-winner)
    /// before the tolerance is calibrated.
    ///
    /// No question is asked when an option cannot be named without leaking an internal snake_case skill
    /// name, none when both options would be named identically - two CRUD descriptions can share a first
    /// sentence, and "do you mean X or X?" is a question the user cannot answer - and none when the
    /// installation's language has no authored sentence: an English question in a non-English
    /// installation breaks the one-language rule, so the turn falls back to an ordinary answer with the
    /// note instead.
    /// </summary>
    /// <param name="orderedCandidates">Deterministic candidates, best retrieval score first</param>
    /// <param name="previousLabel">User-facing label of what the previous turn did (rule 1)</param>
    /// <param name="language">Active language of the turn the question is asked in</param>
    private static string? BuildClarification(
        IReadOnlyList<LLMFunction> orderedCandidates, string previousLabel, string? language)
    {
        if (orderedCandidates.Count < GracefulCorrectionDefaults.ClarificationCandidateCount)
        {
            return null;
        }

        var best = orderedCandidates[0].RetrievalScore;
        var runnerUp = orderedCandidates[1].RetrievalScore;

        var ambiguous = best.HasValue && runnerUp.HasValue
            ? best.Value - runnerUp.Value <= GracefulCorrectionDefaults.CorrectionAmbiguityTolerance
            : !best.HasValue && !runnerUp.HasValue;

        if (!ambiguous)
        {
            return null;
        }

        var firstLabel = DescribeFunction(orderedCandidates[0]);
        var secondLabel = DescribeFunction(orderedCandidates[1]);
        if (firstLabel == null || secondLabel == null
            || string.Equals(firstLabel, secondLabel, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!GracefulCorrectionTexts.TryGetText(
                GracefulCorrectionTexts.ClarificationQuestion, language, out var template))
        {
            return null;
        }

        return template
            .Replace(GracefulCorrectionTexts.PreviousActionPlaceholder, previousLabel, StringComparison.Ordinal)
            .Replace(GracefulCorrectionTexts.FirstOptionPlaceholder, firstLabel, StringComparison.Ordinal)
            .Replace(GracefulCorrectionTexts.SecondOptionPlaceholder, secondLabel, StringComparison.Ordinal);
    }

    /// <summary>
    /// A user-facing label for a candidate: the first sentence of its description, capped. Never the
    /// internal snake_case name - InternalIdentifierRedactor exists precisely because those must not
    /// reach a user. Null when the skill carries no description.
    /// </summary>
    /// <param name="function">The candidate whose description is turned into an option label</param>
    private static string? DescribeFunction(LLMFunction function) =>
        FirstSentenceLabel(function.Description, GracefulCorrectionDefaults.OptionLabelMaxLength);

    /// <summary>
    /// The whole phrase the note substitutes for its language slot. Never empty: a blank tag would read
    /// as "Answer in ." and the model would fall back to guessing, which is what the one-language rule
    /// exists to prevent. A turn without a language does NOT get a default tag either - ordering English
    /// for a user writing German would break the same rule from the other side - it is pointed at the
    /// user's own message instead. A phrase rather than a bare tag because only the named-tag half reads
    /// correctly in quotes, so the quoting lives here and not in the template.
    /// </summary>
    private static string AnswerLanguage(string? language) =>
        string.IsNullOrWhiteSpace(language)
            ? GracefulCorrectionNotes.LanguageOfTheUserMessage
            : string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                GracefulCorrectionNotes.NamedLanguageTemplate,
                language);

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

        return FirstSentenceLabel(function?.Description, GracefulCorrectionDefaults.SkillDisplayLabelMaxLength);
    }

    /// <summary>
    /// The shared shape of both user-facing labels: the first sentence of a skill description, trimmed
    /// and capped. The two callers differ only in their cap - the stored label of a recorded call is
    /// sized like the answer excerpt, an option label like the question it has to fit into - so the cap
    /// is a parameter rather than a second copy of this code.
    /// </summary>
    /// <param name="description">Skill description the label is taken from</param>
    /// <param name="maxLength">Maximum number of characters the label may have</param>
    private static string? FirstSentenceLabel(string? description, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var sentenceEnd = description.IndexOf('.');
        var label = (sentenceEnd > 0 ? description[..sentenceEnd] : description).Trim();

        return label.Length <= maxLength ? label : label[..maxLength].TrimEnd();
    }
}
