// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Full result of one turn-selection eval run for a single model: the persisted EvalRun
/// aggregate plus the per-item breakdown returned to the caller (not persisted).
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public class TurnEvalRunResult
{
    public EvalRun Run { get; set; } = new();

    public TurnEvalDimensions? Dimensions { get; set; }

    public List<TurnEvalItemResult> Items { get; set; } = new();

    /// <summary>
    /// The same dimensions computed over the items of each goldset origin present in the run. Not persisted
    /// (the persisted aggregate is the whole run). It can be rebuilt after the fact only by reloading the goldset
    /// (for the Source of each item id) and re-scoring the stored ToolSequenceJson and ResponseText of the
    /// eval_run_items rows; the rows themselves carry no origin.
    /// </summary>
    public IReadOnlyDictionary<TurnGoldsetOrigin, TurnEvalDimensions> DimensionsByOrigin { get; set; } =
        new Dictionary<TurnGoldsetOrigin, TurnEvalDimensions>();

    /// <summary>
    /// The same dimensions computed over the items of each locale present in the run, so a regression in one
    /// language is not averaged away by the German majority. Not persisted, like DimensionsByOrigin; logged per
    /// locale by TurnEvalRunnerService.
    /// </summary>
    public IReadOnlyDictionary<string, TurnEvalDimensions> DimensionsByLocale { get; set; } =
        new Dictionary<string, TurnEvalDimensions>();
}
