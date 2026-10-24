// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Aggregated scorecard of one turn-selection eval run for a single model. Null accuracy
/// values mean the goldset contained no items of that category, so the dimension was not
/// measured and its weight is redistributed in the composite. AvgLatencyMs is reported and
/// persisted but is NOT part of the composite (see TurnEvalScorer), and neither are RetrievalHit
/// and SelectionHit: they split ToolAccuracy into its two causes for diagnosis and for the
/// regression alert, they are not a second quality target. CorrectionHit, FalseRepairRate and
/// UndoOfferedWhenExpected (TP1) are reported the same way and are likewise outside the composite:
/// the correction goldset is a separate file, so mixing its rates into a weighted mean of a different
/// goldset's dimensions would compare two populations. Both are optional parameters so a run
/// persisted before they existed still deserializes. ReachedHit and LookupDetourRate (scorer version 5)
/// are reported the same way and stay outside the composite. RightsRefusalAccuracy (share of rights-refusal items
/// without a side-effecting call and without an invented result), RightsNoWriteRate (the no-write part alone),
/// RefusalStatedRate (share of those that ended without a tool call and carried a recognisable refusal phrase) and
/// RightsToolForcedRate (share of rights-refusal items on which production forces a tool call, tool_choice required,
/// so a refusal text is impossible by construction) are likewise outside the composite and omitted from the persisted JSON while null,
/// so the dimensions of every run without rights items serialize exactly as before.
/// UnexpectedRecipeRate (2026-10-24) is the share of items expecting NO recipe on which a recipe would force or
/// engage. It is computed over all non-errored items because those turns are what Excluded removes from every
/// other dimension - without it a rising hijack rate only showed as a smaller denominator. Outside the composite.
/// </summary>

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public sealed record TurnEvalDimensions(
    double? ToolAccuracy,
    double? SlotAccuracy,
    double? NoToolAccuracy,
    double? NameResolutionAccuracy,
    double AvgLatencyMs,
    decimal TotalCost,
    int ItemsTotal,
    int ItemsPassed,
    int ItemsExcluded,
    int ItemsErrored,
    double? HonestyAccuracy = null,
    double? RecipeAccuracy = null,
    double? RetrievalHit = null,
    double? SelectionHit = null,
    double? CorrectionHit = null,
    double? FalseRepairRate = null,
    double? UndoOfferedWhenExpected = null,
    double? ReachedHit = null,
    double? LookupDetourRate = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? RightsRefusalAccuracy = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? RefusalStatedRate = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? RightsNoWriteRate = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? RightsToolForcedRate = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? UnexpectedRecipeRate = null);
