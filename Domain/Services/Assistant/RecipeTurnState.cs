// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The recipe bookkeeping of one chat turn: which plan (if any) drives the turn, the run row that carries
/// the started - completed/aborted lifecycle across turns, whether the turn paused on an ask or a
/// confirmation, and how the run is closed when the turn ends. Both chat loops (streaming and
/// non-streaming) carried this as eleven parallel local variables plus four hand-copied blocks, so any
/// change to the lifecycle had to be made twice or it silently reached only one path. The pause, gate-hold
/// and finalize transitions are methods here; only the emission of text belongs to the loops.
/// </summary>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

internal sealed class RecipeTurnState
{
    private readonly IRecipeRunRecorder _recorder;
    private readonly RecipeEngineService _engine;
    private readonly ILogger _logger;
    private readonly LLMContext _context;
    private readonly string _conversationId;

    private string? _abortReason;

    private bool _sealStopNoted;

    private bool _sealStopNoteDue;

    private RecipeTurnState(
        IRecipeRunRecorder recorder,
        RecipeEngineService engine,
        ILogger logger,
        LLMContext context,
        string conversationId,
        TurnPreparation preparation,
        RecipeForcingPlan? cutPlan,
        RecipeRunHandle? run,
        Guid userGuid,
        bool suggestPlan)
    {
        _recorder = recorder;
        _engine = engine;
        _logger = logger;
        _context = context;
        _conversationId = conversationId;
        Plan = preparation.Plan;
        CutPlan = cutPlan;
        Forcing = (IRecipeForcingPlan?)preparation.Plan ?? cutPlan;
        Run = run;
        UserGuid = userGuid;
        SuggestPlan = suggestPlan;
        ForceConfirm = preparation.ForceConfirm;
        ConfirmFunction = preparation.ConfirmFunction;
        PendingNote = preparation.ForceConfirm ? preparation.VolatileNote : null;
        AmbiguousReplyNote = preparation.ForceConfirm ? null : preparation.VolatileNote;
    }

    /// <summary>The data-driven recipe plan of this turn, null when no recipe was resolved or resumed.</summary>
    internal RecipeExecutionPlan? Plan { get; }

    /// <summary>
    /// The hard-coded cut plan, resolved only when no data-driven plan applies: the continuation of a turn that redeems
    /// the recipe's held seal (TurnPreparation.CutContinuation), otherwise a fresh match on the message.
    /// </summary>
    internal RecipeForcingPlan? CutPlan { get; }

    /// <summary>
    /// The plan that narrows the toolset, i.e. the engine plan when present and the cut plan otherwise.
    /// Becomes null once the autonomy gate holds a skill, which releases the forcing for the rest of the turn.
    /// </summary>
    internal IRecipeForcingPlan? Forcing { get; private set; }

    /// <summary>The run row opened or resumed for this turn, null when nothing could be attributed.</summary>
    internal RecipeRunHandle? Run { get; }

    internal Guid UserGuid { get; }

    /// <summary>True when the plan-trigger heuristic considers this turn a plan candidate.</summary>
    internal bool SuggestPlan { get; }

    /// <summary>True when an outstanding confirmation must be forced this turn.</summary>
    internal bool ForceConfirm { get; }

    /// <summary>The single tool the turn is narrowed to while the confirmation gate applies.</summary>
    internal LLMFunction? ConfirmFunction { get; }

    /// <summary>The volatile note belonging to the confirmation gate.</summary>
    internal string? PendingNote { get; }

    /// <summary>
    /// The note that tells the model the user's reply was ambiguous between languages and has to be asked again;
    /// set only on a turn that did not force the confirmation.
    /// </summary>
    internal string? AmbiguousReplyNote { get; }

    /// <summary>True once the turn stopped on a recipe ask or confirmation step.</summary>
    internal bool PausedOnAsk { get; private set; }

    /// <summary>The slot the turn asked for, used to ground the answer's suggestion chips.</summary>
    internal string? AskedSlot { get; private set; }

    /// <summary>
    /// True for exactly one read after the autonomy gate ended an active recipe: the next tool-result
    /// message carries the note that tells the model the recipe is over.
    /// </summary>
    internal bool GateHoldNoteDue { get; private set; }

    /// <summary>
    /// True once a read-only recipe (only ask/search steps) executed its final step in this turn. From then on
    /// the turn's reply must not write anything (ReadOnlyRecipeWriteGuard) or claim that it did.
    /// </summary>
    internal bool ReadOnlyRecipeCompleted => Plan is { CompletedThisTurn: true, IsReadOnly: true };

    /// <summary>
    /// True once any recipe (read-only or writing) executed its final step in this turn. A final step held by the
    /// autonomy gate does not count: its call returns a confirmation, which is not a success, so the plan never
    /// observes it as executed.
    /// </summary>
    internal bool RecipeCompleted => Plan is { CompletedThisTurn: true };

    /// <summary>
    /// The note that carries the final step's reply instructions into the reply call of the turn that completed
    /// the recipe, or null when no recipe completed this turn. A read-only recipe gets a prefix saying that
    /// nothing was stored; a writing recipe gets one saying that its last step already ran and the note only tells
    /// the reply what to contain (its notes were written for the forced call - "add ... now" - and used to reach
    /// the reply call never, so "relay the verified count" instructions were lost).
    /// </summary>
    internal string? CompletionNote => !RecipeCompleted
        ? null
        : (Plan!.IsReadOnly
            ? RecipeEngineDefaults.ReadOnlyRecipeCompletedNotePrefix
            : RecipeEngineDefaults.WritingRecipeCompletedNotePrefix) + Plan.CompletionNote;

    /// <summary>
    /// Rejects the side-effecting calls of an iteration that follows a completed recipe (ReadOnlyRecipeWriteGuard)
    /// or a name ask-back earlier in the turn (NameAskBackWriteGuard) and returns the calls that may execute.
    /// </summary>
    /// <param name="executableCalls">The calls the repeat guard already let through.</param>
    internal List<LLMFunctionCall> RejectWrites(List<LLMFunctionCall> executableCalls) =>
        NameAskBackWriteGuard.Reject(
            ReadOnlyRecipeWriteGuard.Reject(
                executableCalls,
                RecipeCompleted,
                ReadOnlyRecipeCompleted
                    ? LLMLoopConstants.ReadOnlyRecipeWriteRejectedResult
                    : LLMLoopConstants.CompletedRecipeWriteRejectedResult,
                _context.AvailableFunctions),
            NameAskBackSeen,
            _context.AvailableFunctions);

    /// <summary>
    /// True once a call of this turn answered with a name-resolution ask-back (NameResolutionAskBack), failed or
    /// successful (a search that only found similar names returns its suggestions as a success). From then on
    /// RejectWrites rejects every side-effecting call of the turn (NameAskBackWriteGuard), so neither the asked
    /// step nor any other write - for example one by id after a lenient read - runs before the user answered.
    /// </summary>
    internal bool NameAskBackSeen { get; private set; }

    /// <summary>
    /// Feeds the executed calls of an iteration to the forcing plan and handles a name-resolution ask-back: the
    /// turn is marked (NameAskBackSeen), and a failed step is not advanced, so the next iteration would force the
    /// very same skill with tool_choice=required and the model could only call it again with the suggested name.
    /// When the asked call is the current step of a data-driven recipe whose name comes from a question step, the
    /// recipe rewinds to that question (the turn asks the user, the recipe continues with the answer); otherwise
    /// the forcing is released and the run is recorded as aborted. Every other failure keeps the forcing, so a
    /// step that failed for a fixable reason (for example a date format) is still retried with corrected
    /// parameters in the same turn. A successful call carrying the marker text (a search that only found similar
    /// names, such as search_employees or list_groups) also marks the turn, so no write can act on the suggested
    /// name before the user confirmed it; the plan already rewound its own search step through the failed capture,
    /// and a forcing that would still drive a further step is released because that step could not write anyway.
    /// </summary>
    /// <param name="executedCalls">All calls of the iteration, after execution.</param>
    internal void ObserveExecuted(IReadOnlyList<LLMFunctionCall> executedCalls)
    {
        Forcing?.Observe(executedCalls);
        if (!_sealStopNoted && CutPlan is { IsStoppedAtSeal: true })
        {
            _sealStopNoted = true;
            _sealStopNoteDue = true;
        }
        var askBacks = executedCalls
            .Where(call => !call.IsRejectedWithoutRunning && NameResolutionAskBack.IsAskBack(call.Result))
            .ToList();
        if (askBacks.Count == 0)
        {
            return;
        }

        NameAskBackSeen = true;
        if (Forcing == null)
        {
            return;
        }

        var askBack = askBacks.FirstOrDefault(call => !call.Success);
        if (askBack == null)
        {
            if (Forcing.IsActive && Forcing.CurrentSkill != null)
            {
                ReleaseOnNameAskBack();
            }

            return;
        }

        if (Plan is { IsActive: true } plan
            && string.Equals(askBack.FunctionName, plan.CurrentSkill, StringComparison.OrdinalIgnoreCase)
            && plan.TryRewindCurrentStepAfterNameAskBack())
        {
            _logger.LogInformation(
                "Recipe '{Recipe}' rewound to the question of its step after a name ask-back of {Skill}",
                plan.Name, askBack.FunctionName);
            return;
        }

        ReleaseOnNameAskBack();
    }

    /// <summary>
    /// Recipe forcing spine shared by both chat loops, so a hook can never land on only one path: while a
    /// recipe plan is active and a confirmation is not already being forced, the iteration's tool scope is
    /// narrowed to the recipe's current step skill and the step is reported as forced (the caller sets
    /// tool_choice=required and appends the step note). This forces the ordered chain step by step.
    /// </summary>
    /// <param name="confirmThisIteration">True while the pending-confirmation gate narrows the iteration.</param>
    /// <param name="availableFunctions">The turn's full toolset, searched for the step skill.</param>
    /// <param name="iterationFunctions">The toolset the iteration would use without forcing.</param>
    internal (bool Forcing, List<LLMFunction> Functions, string? StepNote) ResolveIteration(
        bool confirmThisIteration,
        List<LLMFunction> availableFunctions,
        List<LLMFunction> iterationFunctions)
    {
        if (confirmThisIteration || Forcing?.IsActive != true)
        {
            return (false, iterationFunctions, null);
        }

        var recipeFunction = availableFunctions.FirstOrDefault(
            f => string.Equals(f.Name, Forcing.CurrentSkill, StringComparison.OrdinalIgnoreCase));
        if (recipeFunction == null)
        {
            // An ask step has no skill: a turn that runs while the plan waits on its question (the topic-switch
            // turn) must leave the plan alone. Only a step that names a skill the toolset lacks ends the recipe.
            if (!string.IsNullOrWhiteSpace(Forcing.CurrentSkill))
            {
                ReleaseOnMissingStepSkill(Forcing.CurrentSkill);
            }

            return (false, iterationFunctions, null);
        }

        return (true, new List<LLMFunction> { recipeFunction }, Forcing.CurrentStepNote);
    }

    /// <summary>
    /// The step skill is not in this turn's toolset, so the step cannot be forced. Logged as a warning because it
    /// means the toolset and the recipe disagree, which the guarantees are meant to prevent; the plan is ended and
    /// the run recorded as aborted, and the turn continues with the full toolset as an ordinary turn.
    /// </summary>
    /// <param name="missingSkill">The step skill that was not found.</param>
    private void ReleaseOnMissingStepSkill(string? missingSkill)
    {
        _logger.LogWarning(
            "Recipe forcing released: step skill {Skill} of recipe {Recipe} is not in the turn's toolset",
            missingSkill, Plan?.Name ?? _context.ActiveRecipeName);
        var wasActive = Plan is { IsActive: true };
        Plan?.DeactivateOnMissingStepSkill();
        Forcing = null;
        if (wasActive)
        {
            _abortReason ??= RecipeAbortReasons.StepSkillMissingFromToolset;
        }
    }

    /// <summary>
    /// Runs the shared pre-loop preparation and opens the run row. The write to
    /// <see cref="LLMContext.ActiveRecipeName"/> happens here because the post-turn hooks read the name
    /// off the very same context instance, for cut plans just as much as for data-driven ones (W1.5). No run
    /// row is opened once a stop was requested, because the turn ends without running the recipe and nothing
    /// would close the row.
    /// </summary>
    /// <param name="turnPreparation">Produces the plan, the confirmation decision and its note.</param>
    /// <param name="recorder">Recipe-run telemetry sink; every call is best-effort.</param>
    /// <param name="engine">Holds the pending-recipe store the plan is persisted to and cleared from.</param>
    /// <param name="logger">The chat service's logger, so log categories stay unchanged.</param>
    /// <param name="context">The turn context, source of the message and target of ActiveRecipeName.</param>
    /// <param name="provider">The provider, needed for the preparation's own model calls.</param>
    /// <param name="model">The model, needed for the preparation's own model calls.</param>
    /// <param name="conversationId">The resolved conversation id the plan and the run are keyed by.</param>
    /// <param name="cancellationToken">Cancels the preparation and the run write.</param>
    internal static async Task<RecipeTurnState> BeginAsync(
        ITurnPreparationService turnPreparation,
        IRecipeRunRecorder recorder,
        RecipeEngineService engine,
        ILogger logger,
        LLMContext context,
        ILLMProvider provider,
        LLMModel model,
        string conversationId,
        CancellationToken cancellationToken)
    {
        var preparation = await turnPreparation.PrepareAsync(
            new TurnPreparationRequest(context, provider, model, conversationId), cancellationToken);
        var cutPlan = preparation.Plan == null
            ? preparation.CutContinuation ?? RecipeForcingResolver.Resolve(context.Message)
            : null;
        var forcing = (IRecipeForcingPlan?)preparation.Plan ?? cutPlan;

        context.ActiveRecipeName = forcing?.Name;
        var suggestPlan = PlanTriggerHeuristic.IsPlanCandidate(context.Message, forcing != null);
        Guid.TryParse(context.UserId, out var userGuid);
        var run = forcing != null && userGuid != Guid.Empty && !context.StopToken.IsCancellationRequested
            ? await recorder.BeginOrResumeAsync(
                forcing.Name, userGuid, conversationId, context.TurnId, forcing.StepIndex, cancellationToken)
            : null;

        return new RecipeTurnState(
            recorder, engine, logger, context, conversationId, preparation, cutPlan, run, userGuid, suggestPlan);
    }

    /// <summary>
    /// Records that the turn paused on the confirmation gate: the plan is persisted for the next turn, the
    /// context is flagged so the answer is read as a confirmation question, and the run's step advances.
    /// </summary>
    internal async Task PauseForConfirmationAsync(CancellationToken cancellationToken)
    {
        _engine.Persist(UserGuid, _conversationId, Plan!);
        PausedOnAsk = true;
        _context.RecipePausedOnAsk = true;
        _context.RecipeAwaitingConfirmation = true;
        await AdvanceRunAsync(cancellationToken);
        _logger.LogInformation("Recipe '{Recipe}' paused for confirmation (semantic match)", Plan!.Name);
    }

    /// <summary>Records that the turn paused on an ask step, capturing the slot it asked for.</summary>
    internal async Task PauseOnAskAsync(CancellationToken cancellationToken)
    {
        await PauseOnSlotAsync(cancellationToken);
        _logger.LogInformation("Recipe '{Recipe}' paused on ask step (slot {Slot})", Plan!.Name, AskedSlot);
    }

    /// <summary>
    /// Records the deterministic re-ask that follows a turn which answered an independent question while
    /// an ask step stayed open. Identical bookkeeping to <see cref="PauseOnAskAsync"/>, different log line.
    /// </summary>
    internal async Task PauseOnReaskAsync(CancellationToken cancellationToken)
    {
        await PauseOnSlotAsync(cancellationToken);
        _logger.LogInformation(
            "Recipe '{Recipe}' re-asked after answering an independent question mid-flow (slot {Slot})",
            Plan!.Name, AskedSlot);
    }

    /// <summary>
    /// Releases the recipe forcing because the autonomy gate held a skill: the model must now ask the user,
    /// so no further iteration may be narrowed to a step skill.
    /// </summary>
    internal void ReleaseOnAutonomyGateHold()
    {
        _logger.LogInformation(
            "Recipe forcing released: a skill was held by the autonomy gate — the model must now ask the user");
        var wasActive = Plan is { IsActive: true };
        GateHoldNoteDue = wasActive;
        Plan?.DeactivateOnAutonomyGateHold();
        Forcing = null;
        if (wasActive)
        {
            _abortReason = RecipeAbortReasons.AutonomyGateHoldEndedRecipe;
        }
    }

    private void ReleaseOnNameAskBack()
    {
        _logger.LogInformation(
            "Recipe forcing released: a skill asked the user to confirm a name — the model must now ask the user");
        var wasActive = Plan is { IsActive: true };
        Plan?.DeactivateOnNameAskBack();
        Forcing = null;
        if (wasActive)
        {
            _abortReason ??= RecipeAbortReasons.NameAskBackEndedRecipe;
        }
    }

    /// <summary>
    /// Returns the one-shot notes due after an iteration, or null when none is: that the recipe ended on a gate hold, and
    /// that the cut recipe stopped on its seal (RecipeConstants.CutRecipeSealStoppedNote: what is still open and that the
    /// model asks for a missing group, so the recipe never ends silently). Consuming them clears the flags, so each note
    /// is appended to exactly one tool-result message.
    /// </summary>
    internal string? TakeGateHoldNote()
    {
        var note = GateHoldNoteDue ? RecipeEngineDefaults.GateHoldEndsRecipeNote : null;
        GateHoldNoteDue = false;
        if (_sealStopNoteDue)
        {
            _sealStopNoteDue = false;
            note += RecipeConstants.CutRecipeSealStoppedNote;
        }

        return note;
    }

    /// <summary>
    /// Closes the run row and clears the pending plan when the turn ended without pausing. A turn that
    /// paused leaves both alone: the pending store carries the plan and the Running row to the next turn. A cut plan
    /// held on its seal also leaves the Running row alone; the turn that confirms the seal resumes it.
    /// </summary>
    internal async Task FinalizeAsync(CancellationToken cancellationToken)
    {
        if (Run != null && !PausedOnAsk)
        {
            if (_abortReason != null)
            {
                await _recorder.AbortAsync(Run, _abortReason, cancellationToken);
            }
            else if (Plan != null)
            {
                if (!Plan.IsActive)
                {
                    await _recorder.CompleteAsync(Run, cancellationToken);
                }
            }
            else if (CutPlan is { IsDeactivated: true })
            {
                await _recorder.AbortAsync(Run, RecipeAbortReasons.AmbiguousCutRecipeMatch, cancellationToken);
            }
            else if (CutPlan is { IsStoppedAtSeal: true })
            {
                await _recorder.AbortAsync(Run, RecipeAbortReasons.CutRecipeStoppedAtSeal, cancellationToken);
            }
            else if (CutPlan is { AwaitsConfirmation: true })
            {
                // The gate held the seal: the run stays Running, the turn that confirms the seal resumes it
                // (CutRecipeContinuationResolver) and closes it.
                _logger.LogInformation("Recipe '{Recipe}' waits for the confirmation of its seal", CutPlan.Name);
            }
            else if (CutPlan is { IsActive: false })
            {
                await _recorder.CompleteAsync(Run, cancellationToken);
            }
            else
            {
                await _recorder.AbortAsync(Run, RecipeAbortReasons.CutRecipeIncomplete, cancellationToken);
            }
        }

        if (Plan != null && !PausedOnAsk && !Plan.IsActive)
        {
            _engine.Clear(UserGuid, _conversationId);
        }
    }

    private async Task PauseOnSlotAsync(CancellationToken cancellationToken)
    {
        AskedSlot = Plan!.CurrentStep?.Slot;
        _engine.Persist(UserGuid, _conversationId, Plan);
        PausedOnAsk = true;
        _context.RecipePausedOnAsk = true;
        await AdvanceRunAsync(cancellationToken);
    }

    private async Task AdvanceRunAsync(CancellationToken cancellationToken)
    {
        if (Run != null)
        {
            await _recorder.UpdateStepAsync(Run, Plan!.StepIndex, cancellationToken);
        }
    }
}
