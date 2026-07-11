// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Per-item outcome of a speech-wer eval run: either skipped (audio file missing) or
/// measured with word error rate, name accuracy, composite score and transcription latency.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval;

public class SpeechWerEvalItemResult
{
    public string ItemId { get; set; } = string.Empty;

    public string AudioFile { get; set; } = string.Empty;

    public bool Skipped { get; set; }

    public double? Wer { get; set; }

    public double? NameAccuracy { get; set; }

    public double? Composite { get; set; }

    public double LatencyMs { get; set; }

    public string? Transcript { get; set; }
}
