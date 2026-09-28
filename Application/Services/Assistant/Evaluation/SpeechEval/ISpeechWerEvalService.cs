// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval;

public interface ISpeechWerEvalService
{
    Task<SpeechWerEvalRunResult> RunAsync(string sttModelOrProviderId, CancellationToken cancellationToken = default);
}
