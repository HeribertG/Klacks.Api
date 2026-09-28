// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Aggregated scorecard of one speech-wer eval run for a single STT provider. Null averages
/// mean no goldset item had a matching audio file, so nothing was measured.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval;

public sealed record SpeechWerEvalDimensions(
    double? AvgWer,
    double? NameAccuracy,
    int ItemsTotal,
    int ItemsMeasured,
    int ItemsSkipped,
    double AvgLatencyMs);
