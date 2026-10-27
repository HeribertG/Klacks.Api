// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Per-item outcome of a turn-selection eval: what the model chose versus what the
/// goldset expected, with slot-level scoring detail for diagnosis.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public class TurnEvalItemResult
{
    public string ItemId { get; set; } = string.Empty;

    public string? ExpectedTool { get; set; }

    /// <summary>W0.5: expected deterministic recipe for this item, mirrored from the goldset item.</summary>
    public string? ExpectedRecipe { get; set; }

    public string? ChosenTool { get; set; }

    public bool? ToolHit { get; set; }

    /// <summary>W0.5: true when the expected recipe would force/trigger this turn.</summary>
    public bool? RecipeHit { get; set; }

    public bool? NoToolCorrect { get; set; }

    public bool? HonestyCorrect { get; set; }

    public List<string> UngroundedClaims { get; set; } = new();

    public double? SlotScore { get; set; }

    public int NameSlotsEvaluated { get; set; }

    public int NameSlotsResolved { get; set; }

    public bool Passed { get; set; }

    public bool Excluded { get; set; }

    public bool Errored { get; set; }

    public string? Error { get; set; }

    public bool RecipeWouldForce { get; set; }

    public bool EngineRecipeWouldTrigger { get; set; }

    /// <summary>
    /// Whether the expected tool (or one of its alternatives) was in the assembled toolset at all. Null
    /// when the item expects no tool, or when the replay reported no toolset. A miss here is a retrieval
    /// problem, not a model problem.
    /// </summary>
    public bool? RetrievalHit { get; set; }

    /// <summary>
    /// Whether the model picked the expected tool, measured ONLY where the verdict can mean something:
    /// <see cref="RetrievalHit"/> is true, the replay succeeded and no recipe excluded the item. Null
    /// everywhere else - a model that was never offered the tool, never answered at all or never got to
    /// choose cannot be judged on not choosing it.
    /// </summary>
    public bool? SelectionHit { get; set; }

    /// <summary>
    /// Whether the model arrived at the expected tool within the replay's two steps. Same population
    /// as <see cref="SelectionHit"/> and identical to it whenever no second step ran; additionally null
    /// when the second provider call failed. SelectionHit stays the strict first-choice verdict the
    /// learning loop reads - this one answers whether the turn would have ended correctly.
    /// </summary>
    public bool? ReachedHit { get; set; }

    /// <summary>True when <see cref="ReachedHit"/> is true only because of the second step.</summary>
    public bool ReachedViaFollowUp { get; set; }

    /// <summary>
    /// Legacy name of <see cref="RetrievalHit"/>, kept because the live scorecard of the integration
    /// eval prints it on every MISS line. Always the identical value; never computed twice.
    /// </summary>
    public bool? ExpectedToolAvailable { get; set; }

    /// <summary>
    /// TP1: for an item that expects a correction, whether the corrected turn reached the expected
    /// skill (or offered the expected clarification). Null when the item declares no previousTurn or
    /// does not expect a correction.
    /// </summary>
    public bool? CorrectionHit { get; set; }

    /// <summary>
    /// TP1: for an item with a previousTurn that must NOT be repaired, whether the pipeline repaired
    /// it anyway. Null when the item declares no previousTurn or expects a correction.
    /// </summary>
    public bool? FalseRepair { get; set; }

    /// <summary>
    /// TP1: whether the expected inverse skill was offered as an undo. Null when the item expects none.
    /// </summary>
    public bool? UndoOfferedWhenExpected { get; set; }

    /// <summary>Where the goldset item came from, copied from its Source so a run can be reported per origin.</summary>
    public TurnGoldsetOrigin Origin { get; set; }

    /// <summary>The item's locale, copied from the goldset so a run can be reported per language; null when unset.</summary>
    public string? Locale { get; set; }

    /// <summary>The role the item was replayed as; null when it ran with the rights of the whole run.</summary>
    public TurnGoldsetRole? Role { get; set; }

    /// <summary>True for an item whose request exceeds its role's rights; it is scored on the rights verdicts only.</summary>
    public bool ExpectsRightsRefusal { get; set; }

    /// <summary>
    /// For a rights-refusal item: true when no side-effecting call happened in any replay step and a turn that
    /// ended without a tool call did not invent a result (ungrounded claim or completion claim). Gates Passed.
    /// Null when the replay failed.
    /// </summary>
    public bool? RightsRefusalCorrect { get; set; }

    /// <summary>For a rights-refusal item: true when no side-effecting call happened. Null when the replay failed.</summary>
    public bool? NoWriteCall { get; set; }

    /// <summary>
    /// Whether the replay forced a tool call (tool_choice required), exactly as the production chat loop does for a
    /// state-changing intent. On a rights-refusal item this means the model could not answer with a plain refusal.
    /// </summary>
    public bool ToolChoiceRequired { get; set; }

    /// <summary>
    /// For a rights-refusal item that ended without any tool call: whether the answer contains a recognisable
    /// refusal phrase. A lexical lower bound - a correct refusal in unforeseen wording reads as false - so it is
    /// reported and never gates Passed. Null when a tool was called (the answer then is not the final word) or
    /// the replay failed.
    /// </summary>
    public bool? RefusalStated { get; set; }

    /// <summary>
    /// Why the item hit or missed, set by the runner after scoring (see <see cref="TurnEvalMissClassifier"/>). A label
    /// only: no verdict, dimension or composite reads it. Null until the runner classified the item.
    /// </summary>
    public EvalMissKind? MissKind { get; set; }

    public long LatencyMs { get; set; }

    public decimal Cost { get; set; }
}
