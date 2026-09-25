// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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

    private bool _abortedByGateHold;

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
        PendingNote = preparation.VolatileNote;
    }

    /// <summary>The data-driven recipe plan of this turn, null when no recipe was resolved or resumed.</summary>
    internal RecipeExecutionPlan? Plan { get; }

    /// <summary>The hard-coded cut plan, resolved only when no data-driven plan applies.</summary>
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
    /// Runs the shared pre-loop preparation and opens the run row. The write to
    /// <see cref="LLMContext.ActiveRecipeName"/> happens here because the post-turn hooks read the name
    /// off the very same context instance, for cut plans just as much as for data-driven ones (W1.5).
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
        var cutPlan = preparation.Plan == null ? RecipeForcingResolver.Resolve(context.Message) : null;
        var forcing = (IRecipeForcingPlan?)preparation.Plan ?? cutPlan;

        context.ActiveRecipeName = forcing?.Name;
        var suggestPlan = PlanTriggerHeuristic.IsPlanCandidate(context.Message, forcing != null);
        Guid.TryParse(context.UserId, out var userGuid);
        var run = forcing != null && userGuid != Guid.Empty
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
        _abortedByGateHold = wasActive;
    }

    /// <summary>
    /// Returns the one-shot note that tells the model its recipe ended on a gate hold, or null when none is
    /// due. Consuming it clears the flag, so the note is appended to exactly one tool-result message.
    /// </summary>
    internal string? TakeGateHoldNote()
    {
        if (!GateHoldNoteDue)
        {
            return null;
        }

        GateHoldNoteDue = false;
        return RecipeEngineDefaults.GateHoldEndsRecipeNote;
    }

    /// <summary>
    /// Closes the run row and clears the pending plan when the turn ended without pausing. A turn that
    /// paused leaves both alone: the pending store carries the plan and the Running row to the next turn.
    /// </summary>
    internal async Task FinalizeAsync(CancellationToken cancellationToken)
    {
        if (Run != null && !PausedOnAsk)
        {
            if (_abortedByGateHold)
            {
                await _recorder.AbortAsync(Run, RecipeAbortReasons.AutonomyGateHoldEndedRecipe, cancellationToken);
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
