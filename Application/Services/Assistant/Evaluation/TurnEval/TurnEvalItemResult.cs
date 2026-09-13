// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
    /// Legacy name of <see cref="RetrievalHit"/>, kept because the live scorecard of the integration
    /// eval prints it on every MISS line. Always the identical value; never computed twice.
    /// </summary>
    public bool? ExpectedToolAvailable { get; set; }

    public long LatencyMs { get; set; }

    public decimal Cost { get; set; }
}
