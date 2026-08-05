// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Aggregated scorecard of one turn-selection eval run for a single model. Null accuracy
/// values mean the goldset contained no items of that category, so the dimension was not
/// measured and its weight is redistributed in the composite.
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
    double? HonestyAccuracy = null);
