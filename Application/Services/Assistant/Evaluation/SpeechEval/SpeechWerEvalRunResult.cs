// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Full result of one speech-wer eval run: the persisted EvalRun aggregate (null when no
/// item could be measured), the aggregated dimensions, the per-item breakdown and an
/// optional message explaining why nothing was persisted.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval;

public class SpeechWerEvalRunResult
{
    public EvalRun? Run { get; set; }

    public SpeechWerEvalDimensions? Dimensions { get; set; }

    public List<SpeechWerEvalItemResult> Items { get; set; } = new();

    public string? Message { get; set; }
}
