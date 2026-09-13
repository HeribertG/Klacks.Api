// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Aggregated scorecard of one turn-selection eval run for a single model. Null accuracy
/// values mean the goldset contained no items of that category, so the dimension was not
/// measured and its weight is redistributed in the composite. AvgLatencyMs is reported and
/// persisted but is NOT part of the composite (see TurnEvalScorer), and neither are RetrievalHit
/// and SelectionHit: they split ToolAccuracy into its two causes for diagnosis and for the
/// regression alert, they are not a second quality target. Both are optional parameters so a run
/// persisted before they existed still deserializes.
/// </summary>

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
    double? SelectionHit = null);
