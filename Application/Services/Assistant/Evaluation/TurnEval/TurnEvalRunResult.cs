// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
}
