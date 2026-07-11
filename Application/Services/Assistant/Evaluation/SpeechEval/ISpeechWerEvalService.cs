// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval;

public interface ISpeechWerEvalService
{
    Task<SpeechWerEvalRunResult> RunAsync(string sttModelOrProviderId, CancellationToken cancellationToken = default);
}
